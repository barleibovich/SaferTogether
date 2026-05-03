const { httpError } = require("./errors");
const { getSessionContext } = require("./supabaseService");

// This function creates a short code for joining a group.
function createJoinCode() {
  return Math.random().toString(36).slice(2, 8).toUpperCase();
}

// This function changes a group from the database to the format the UI uses.
function mapGroup(group, role, pendingRequests = [], members = []) {
  return {
    id: group.id,
    joinCode: group.join_code || "",
    members,
    name: group.name,
    pendingRequests,
    userRole: role
  };
}

// This function makes sure only admins can manage groups.
async function requireAdminContext(accessToken) {
  const context = await getSessionContext(accessToken);

  if (context.profile.role !== "admin") {
    throw httpError(403, "Only admins can manage groups");
  }

  return context;
}

// This function gets one group that belongs to the current admin.
async function getOwnedGroupRecord(client, userId, groupId) {
  const { data, error } = await client
    .from("groups")
    .select("id, name, description, created_by, join_code")
    .eq("id", groupId)
    .eq("created_by", userId)
    .maybeSingle();

  if (error) {
    throw error;
  }

  if (!data) {
    throw httpError(404, "Group not found");
  }

  return data;
}

// This function gets the pending requests for admin groups.
async function getPendingRequests(client, groupIds) {
  if (!groupIds.length) {
    return [];
  }

  const { data, error } = await client
    .from("group_join_requests")
    .select("id, group_id, user_id, status, requested_username")
    .eq("status", "pending")
    .in("group_id", groupIds)
    .order("created_at", { ascending: true });

  if (error) {
    throw error;
  }

  return (data || []).map(request => ({
    groupId: request.group_id,
    id: request.id,
    status: request.status,
    userId: request.user_id,
    username: request.requested_username || ""
  }));
}

// This function gets the members of the groups.
async function getGroupMembers(client, groupIds) {
  if (!groupIds.length) {
    return [];
  }

  const { data, error } = await client
    .from("user_groups")
    .select("group_id, user_id, member_username")
    .in("group_id", groupIds);

  if (error) {
    throw error;
  }

  return (data || []).map(member => ({
    groupId: member.group_id,
    id: member.user_id,
    username: member.member_username || "User"
  }));
}

// This function gets the groups of the current user.
async function getVisibleGroups(accessToken) {
  const context = await getSessionContext(accessToken);

  if (context.profile.role === "admin") {
    const { data, error } = await context.client
      .from("groups")
      .select("id, name, description, created_by, join_code")
      .eq("created_by", context.user.id)
      .order("created_at", { ascending: false });

    if (error) {
      throw error;
    }

    const groups = data || [];
    const members = await getGroupMembers(
      context.client,
      groups.map(group => group.id)
    );
    const pendingRequests = await getPendingRequests(
      context.client,
      groups.map(group => group.id)
    );

    return groups.map(group => (
      mapGroup(
        group,
        "admin",
        pendingRequests.filter(request => request.groupId === group.id),
        members.filter(member => member.groupId === group.id)
      )
    ));
  }

  const { data, error } = await context.client
    .from("user_groups")
    .select(`
      groups (
        id,
        name,
        description,
        created_by,
        join_code
      )
    `)
    .eq("user_id", context.user.id);

  if (error) {
    throw error;
  }

  const groups = (data || []).map(row => row.groups).filter(Boolean);
  const members = await getGroupMembers(
    context.client,
    groups.map(group => group.id)
  );

  return groups.map(group => (
    mapGroup(
      group,
      "user",
      [],
      members.filter(member => member.groupId === group.id)
    )
  ));
}

// This function creates a group and saves a join code for it.
async function createGroupForCurrentUser(accessToken, { name }) {
  const context = await requireAdminContext(accessToken);
  const groupName = String(name || "").trim();

  if (!groupName) {
    throw httpError(400, "Group name is required");
  }

  const { data: group, error } = await context.client
    .from("groups")
    .insert({
      created_by: context.user.id,
      description: "",
      join_code: createJoinCode(),
      name: groupName
    })
    .select("id, name, description, created_by, join_code")
    .single();

  if (error) {
    throw error;
  }

  const { error: memberError } = await context.client
    .from("user_groups")
    .insert({
      group_id: group.id,
      member_username: context.profile.username,
      user_id: context.user.id
    });

  if (memberError) {
    throw memberError;
  }

  return mapGroup(group, "admin");
}

// This function sends a join request by code.
async function requestJoinByCode(accessToken, { code }) {
  const context = await getSessionContext(accessToken);
  const joinCode = String(code || "").trim().toUpperCase();

  if (!joinCode) {
    throw httpError(400, "Team code is required");
  }

  const { data: group, error: groupError } = await context.client
    .from("groups")
    .select("id, name, created_by")
    .eq("join_code", joinCode)
    .maybeSingle();

  if (groupError) {
    throw groupError;
  }

  if (!group) {
    throw httpError(404, "Invalid team code");
  }

  const { data: membership, error: membershipError } = await context.client
    .from("user_groups")
    .select("group_id")
    .eq("group_id", group.id)
    .eq("user_id", context.user.id)
    .maybeSingle();

  if (membershipError) {
    throw membershipError;
  }

  if (membership) {
    throw httpError(400, "You are already in this group");
  }

  const { data: existingRequest, error: requestError } = await context.client
    .from("group_join_requests")
    .select("id, status")
    .eq("group_id", group.id)
    .eq("user_id", context.user.id)
    .maybeSingle();

  if (requestError) {
    throw requestError;
  }

  if (existingRequest?.status === "pending") {
    throw httpError(400, "You already sent a request");
  }

  if (existingRequest?.status === "declined") {
    const { error: deleteError } = await context.client
      .from("group_join_requests")
      .delete()
      .eq("id", existingRequest.id);

    if (deleteError) {
      throw deleteError;
    }
  }

  if (existingRequest?.status === "approved") {
    throw httpError(400, "You are already in this group");
  }

  const requestPayload = {
    group_id: group.id,
    requested_username: context.profile.username,
    user_id: context.user.id
  };

  const { error: insertError } = await context.client
    .from("group_join_requests")
    .insert(requestPayload);

  if (insertError?.code === "23505") {
    const { data: duplicateRequest, error: duplicateError } = await context.client
      .from("group_join_requests")
      .select("id, status")
      .eq("group_id", group.id)
      .eq("user_id", context.user.id)
      .maybeSingle();

    if (duplicateError) {
      throw duplicateError;
    }

    if (duplicateRequest?.status === "pending") {
      throw httpError(400, "You already sent a request");
    }

    if (duplicateRequest?.status === "approved") {
      throw httpError(400, "You are already in this group");
    }

    if (duplicateRequest?.status === "declined") {
      const { error: deleteError } = await context.client
        .from("group_join_requests")
        .delete()
        .eq("id", duplicateRequest.id);

      if (deleteError) {
        throw deleteError;
      }

      const { error: retryError } = await context.client
        .from("group_join_requests")
        .insert(requestPayload);

      if (retryError) {
        throw retryError;
      }
    }
  }

  if (insertError) {
    throw insertError;
  }

  return {
    groupId: group.id,
    groupName: group.name,
    status: "pending"
  };
}

// This function lets the admin approve or decline a request.
async function reviewJoinRequest(accessToken, groupId, requestId, { status }) {
  const context = await requireAdminContext(accessToken);

  if (status !== "approved" && status !== "declined") {
    throw httpError(400, "Invalid request status");
  }

  await getOwnedGroupRecord(context.client, context.user.id, groupId);

  const { data: request, error: requestError } = await context.client
    .from("group_join_requests")
    .select("id, group_id, user_id, requested_username, status")
    .eq("id", requestId)
    .eq("group_id", groupId)
    .maybeSingle();

  if (requestError) {
    throw requestError;
  }

  if (!request) {
    throw httpError(404, "Request not found");
  }

  if (status === "approved") {
    const { data: membership, error: membershipError } = await context.client
      .from("user_groups")
      .select("group_id")
      .eq("group_id", groupId)
      .eq("user_id", request.user_id)
      .maybeSingle();

    if (membershipError) {
      throw membershipError;
    }

    if (!membership) {
      const { error: insertError } = await context.client
        .from("user_groups")
        .insert({
          group_id: groupId,
          member_username: request.requested_username || "User",
          user_id: request.user_id
        });

      if (insertError) {
        throw insertError;
      }
    }
  }

  if (status === "declined") {
    const { error: deleteError } = await context.client
      .from("group_join_requests")
      .delete()
      .eq("id", requestId)
      .eq("group_id", groupId);

    if (deleteError) {
      throw deleteError;
    }

    return {
      groupId,
      id: request.id,
      status,
      userId: request.user_id
    };
  }

  const { data: updatedRequest, error: updateError } = await context.client
    .from("group_join_requests")
    .update({ status })
    .eq("id", requestId)
    .eq("group_id", groupId)
    .select("id, group_id, user_id, status")
    .single();

  if (updateError) {
    throw updateError;
  }

  return {
    groupId: updatedRequest.group_id,
    id: updatedRequest.id,
    status: updatedRequest.status,
    userId: updatedRequest.user_id
  };
}

// This function updates a group that belongs to the current admin.
async function updateOwnedGroup(accessToken, groupId, { name, description }) {
  const context = await requireAdminContext(accessToken);
  await getOwnedGroupRecord(context.client, context.user.id, groupId);

  const payload = {};

  if (typeof name === "string" && name.trim()) {
    payload.name = name.trim();
  }

  if (typeof description === "string") {
    payload.description = description;
  }

  if (!Object.keys(payload).length) {
    throw httpError(400, "Nothing to update");
  }

  const { data: group, error } = await context.client
    .from("groups")
    .update(payload)
    .eq("id", groupId)
    .eq("created_by", context.user.id)
    .select("id, name, description, created_by, join_code")
    .single();

  if (error) {
    throw error;
  }

  return mapGroup(group, "admin");
}

// This function deletes a group that belongs to the current admin.
async function deleteOwnedGroup(accessToken, groupId) {
  const context = await requireAdminContext(accessToken);
  await getOwnedGroupRecord(context.client, context.user.id, groupId);

  const { error: requestsError } = await context.client
    .from("group_join_requests")
    .delete()
    .eq("group_id", groupId);

  if (requestsError) {
    throw requestsError;
  }

  const { error: membershipsError } = await context.client
    .from("user_groups")
    .delete()
    .eq("group_id", groupId);

  if (membershipsError) {
    throw membershipsError;
  }

  const { error: deleteError } = await context.client
    .from("groups")
    .delete()
    .eq("id", groupId)
    .eq("created_by", context.user.id);

  if (deleteError) {
    throw deleteError;
  }

  return { success: true };
}

module.exports = {
  createGroupForCurrentUser,
  deleteOwnedGroup,
  getVisibleGroups,
  requestJoinByCode,
  reviewJoinRequest,
  updateOwnedGroup
};
