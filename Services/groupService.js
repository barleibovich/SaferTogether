const { httpError } = require("./errors");
const { normalizeAvatar } = require("./avatarService");
const { getLocationsForUsers } = require("./memberLocationService");
const { getPresenceForUsers } = require("./presenceService");
const { getSessionContext } = require("./supabaseService");

// make a short join code for a group
function createJoinCode() {
  return Math.random().toString(36).slice(2, 8).toUpperCase();
}

// turn a db group row into the shape the UI wants
function mapGroup(group, role, pendingRequests = [], members = []) {
  return {
    drillActive: group.drill_active || false,
    drillStartedAt: group.drill_started_at || null,
    id: group.id,
    joinCode: group.join_code || "",
    members,
    name: group.name,
    pendingRequests,
    userRole: role
  };
}

// grab the avatar png from a profile, whatever key its under
function profileAvatarImage(profile) {
  return profile?.avatar_image || profile?.avatarImage || "";
}

// only admins can manage groups
async function requireAdminContext(accessToken) {
  const context = await getSessionContext(accessToken);

  if (context.profile.role !== "admin") {
    throw httpError(403, "רק מנהלים יכולים לנהל קבוצות");
  }

  return context;
}

// check if this admin can manage the group
async function getManageableGroupRecord(client, userId, groupId) {
  const { data, error } = await client
    .from("groups")
    .select("id, name, description, created_by, join_code")
    .eq("id", groupId)
    .maybeSingle();

  if (error) {
    throw error;
  }

  if (!data) {
    throw httpError(404, "הקבוצה לא נמצאה");
  }

  if (data.created_by === userId) {
    return data;
  }

  const { data: membership, error: membershipError } = await client
    .from("user_groups")
    .select("group_id")
    .eq("group_id", groupId)
    .eq("user_id", userId)
    .maybeSingle();

  if (membershipError) {
    throw membershipError;
  }

  if (!membership) {
    throw httpError(404, "הקבוצה לא נמצאה");
  }

  return data;
}

// get a group owned by the current admin
async function getOwnedGroupRecord(client, userId, groupId) {
  const group = await getManageableGroupRecord(client, userId, groupId);

  if (group.created_by !== userId) {
    throw httpError(404, "הקבוצה לא נמצאה");
  }

  return group;
}

// get pending join requests for the admin's groups
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

// read profile rows that RLS lets us see
async function getProfilesById(client, userIds) {
  const ids = [...new Set((userIds || []).filter(Boolean))];

  if (!ids.length) {
    return new Map();
  }

  const { data, error } = await client
    .from("profiles")
    .select("*")
    .in("id", ids);

  if (error) {
    return new Map();
  }

  return new Map((data || []).map(profile => [profile.id, profile]));
}

// get the members of the groups
async function getGroupMembers(client, groups, currentUser, currentProfile) {
  const groupIds = (groups || []).map(group => group.id).filter(Boolean);

  if (!groupIds.length) {
    return [];
  }

  const ownerByGroupId = new Map((groups || []).map(group => [group.id, group.created_by]));

  const { data, error } = await client
    .from("user_groups")
    .select("group_id, user_id, member_username")
    .in("group_id", groupIds);

  if (error) {
    throw error;
  }

  const locationMap = await getLocationsForUsers(
    client,
    (data || []).map(member => member.user_id)
  );
  const profileMap = await getProfilesById(
    client,
    (data || []).map(member => member.user_id)
  );
  const presenceMap = await getPresenceForUsers(
    client,
    (data || []).map(member => member.user_id)
  );

  return (data || []).map(member => {
    const profile = profileMap.get(member.user_id);
    const username = member.member_username || profile?.username || "User";
    const isCurrentUser = member.user_id === currentUser?.id;
    const isGroupOwner = member.user_id === ownerByGroupId.get(member.group_id);
    const avatar = isCurrentUser
      ? (currentUser?.user_metadata?.avatar || currentProfile.avatar)
      : profile?.avatar;

    return {
      alertLocation: locationMap.get(member.user_id) || null,
      avatar: normalizeAvatar(avatar, username),
      avatarImage: profileAvatarImage(isCurrentUser ? currentProfile : profile),
      groupId: member.group_id,
      id: member.user_id,
      lastSeenAt: presenceMap.get(member.user_id) || null,
      role: isGroupOwner ? "admin" : (profile?.role || (isCurrentUser ? currentProfile.role : "user")),
      username
    };
  });
}

// old groups might be missing the owner's member row, add it back as admin
async function addMissingGroupOwnersAsMembers(client, groups, members, currentUser, currentProfile) {
  const existingMemberships = new Set(
    (members || []).map(member => `${member.groupId}:${member.id}`)
  );
  const missingOwnerGroups = (groups || []).filter(group => (
    group.created_by && !existingMemberships.has(`${group.id}:${group.created_by}`)
  ));

  if (!missingOwnerGroups.length) {
    return members;
  }

  const ownerIds = missingOwnerGroups.map(group => group.created_by);
  const profileMap = await getProfilesById(client, ownerIds);
  const locationMap = await getLocationsForUsers(client, ownerIds);
  const presenceMap = await getPresenceForUsers(client, ownerIds);
  const ownerMembers = missingOwnerGroups.map(group => {
    const isCurrentUser = group.created_by === currentUser?.id;
    const profile = profileMap.get(group.created_by);
    const username = isCurrentUser
      ? currentProfile.username
      : (profile?.username || "Admin");

    return {
      alertLocation: locationMap.get(group.created_by) || null,
      avatar: normalizeAvatar(
        isCurrentUser ? (currentUser?.user_metadata?.avatar || currentProfile.avatar) : profile?.avatar,
        username
      ),
      avatarImage: profileAvatarImage(isCurrentUser ? currentProfile : profile),
      groupId: group.id,
      id: group.created_by,
      lastSeenAt: presenceMap.get(group.created_by) || null,
      role: "admin",
      username
    };
  });

  return [...members, ...ownerMembers];
}

// get the current user's groups
async function getVisibleGroups(accessToken) {
  const context = await getSessionContext(accessToken);

  if (context.profile.role === "admin") {
    const { data: ownedGroups, error } = await context.client
      .from("groups")
      .select("id, name, description, created_by, join_code, drill_active, drill_started_at")
      .eq("created_by", context.user.id)
      .order("created_at", { ascending: false });

    if (error) {
      throw error;
    }

    const { data: memberRows, error: memberGroupsError } = await context.client
      .from("user_groups")
      .select(`
        groups (
          id,
          name,
          description,
          created_by,
          join_code,
          drill_active,
          drill_started_at
        )
      `)
      .eq("user_id", context.user.id);

    if (memberGroupsError) {
      throw memberGroupsError;
    }

    const groupsById = new Map();

    (ownedGroups || []).forEach(group => {
      groupsById.set(group.id, group);
    });

    (memberRows || [])
      .map(row => row.groups)
      .filter(Boolean)
      .forEach(group => {
        groupsById.set(group.id, group);
      });

    const groups = Array.from(groupsById.values());
    const rawMembers = await getGroupMembers(
      context.client,
      groups,
      context.user,
      context.profile
    );
    const members = await addMissingGroupOwnersAsMembers(
      context.client,
      groups,
      rawMembers,
      context.user,
      context.profile
    );
    const ownedGroupIds = groups
      .filter(group => group.created_by === context.user.id)
      .map(group => group.id);

    const pendingRequests = await getPendingRequests(
      context.client,
      ownedGroupIds
    );

    return groups.map(group => {
      const isOwner = group.created_by === context.user.id;
      return mapGroup(
        group,
        isOwner ? "admin" : "user",
        isOwner ? pendingRequests.filter(request => request.groupId === group.id) : [],
        members.filter(member => member.groupId === group.id)
      );
    });
  }

  const { data, error } = await context.client
    .from("user_groups")
    .select(`
      groups (
        id,
        name,
        description,
        created_by,
        join_code,
        drill_active,
        drill_started_at
      )
    `)
    .eq("user_id", context.user.id);

  if (error) {
    throw error;
  }

  const groups = (data || []).map(row => row.groups).filter(Boolean);
  const rawMembers = await getGroupMembers(
    context.client,
    groups,
    context.user,
    context.profile
  );
  const members = await addMissingGroupOwnersAsMembers(
    context.client,
    groups,
    rawMembers,
    context.user,
    context.profile
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

// create a group + give it a join code
async function createGroupForCurrentUser(accessToken, { name }) {
  const context = await requireAdminContext(accessToken);
  const groupName = String(name || "").trim();

  if (!groupName) {
    throw httpError(400, "יש להזין שם קבוצה");
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

// send a join request using a code
async function requestJoinByCode(accessToken, { code }) {
  const context = await getSessionContext(accessToken);
  const joinCode = String(code || "").trim().toUpperCase();

  if (!joinCode) {
    throw httpError(400, "יש להזין קוד קבוצה");
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
    throw httpError(404, "קוד קבוצה לא תקין");
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
    throw httpError(400, "אתם כבר חברים בקבוצה זו");
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

  const requestPayload = {
    group_id: group.id,
    requested_username: context.profile.username,
    status: "pending",
    user_id: context.user.id
  };

  if (existingRequest?.status === "pending") {
    throw httpError(400, "כבר שלחתם בקשה");
  }

  if (existingRequest) {
    // stale approved/declined row (they left or were declined) -> reopen it as pending
    const { error: updateError } = await context.client
      .from("group_join_requests")
      .update({ requested_username: context.profile.username, status: "pending" })
      .eq("id", existingRequest.id);

    if (updateError) {
      throw updateError;
    }
  } else {
    const { error: insertError } = await context.client
      .from("group_join_requests")
      .insert(requestPayload);

    if (insertError) {
      throw insertError;
    }
  }

  return {
    groupId: group.id,
    groupName: group.name,
    status: "pending"
  };
}

// admin approves or declines a request
async function reviewJoinRequest(accessToken, groupId, requestId, { status }) {
  const context = await requireAdminContext(accessToken);

  if (status !== "approved" && status !== "declined") {
    throw httpError(400, "סטטוס בקשה לא תקין");
  }

  await getManageableGroupRecord(context.client, context.user.id, groupId);

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
    throw httpError(404, "הבקשה לא נמצאה");
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

// update a group owned by the current admin
async function updateOwnedGroup(accessToken, groupId, { name, description }) {
  const context = await requireAdminContext(accessToken);
  await getManageableGroupRecord(context.client, context.user.id, groupId);

  const payload = {};

  if (typeof name === "string" && name.trim()) {
    payload.name = name.trim();
  }

  if (typeof description === "string") {
    payload.description = description;
  }

  if (!Object.keys(payload).length) {
    throw httpError(400, "אין מה לעדכן");
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

// let a member leave a group (admins cant leave their own)
async function leaveGroup(accessToken, groupId) {
  const context = await getSessionContext(accessToken);

  const { data: group, error: groupError } = await context.client
    .from("groups")
    .select("id, created_by")
    .eq("id", groupId)
    .maybeSingle();

  if (groupError) throw groupError;
  if (!group) throw httpError(404, "הקבוצה לא נמצאה");

  if (group.created_by === context.user.id) {
    throw httpError(403, "מנהל קבוצה אינו יכול לעזוב את הקבוצה שלו — יש למחוק את הקבוצה במקום זאת");
  }

  const { error: deleteError } = await context.client
    .from("user_groups")
    .delete()
    .eq("group_id", groupId)
    .eq("user_id", context.user.id);

  if (deleteError) throw deleteError;

  return { success: true };
}

// delete a group owned by the current admin
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

// start a drill for a group (admin only)
async function startGroupDrill(accessToken, groupId) {
  const context = await requireAdminContext(accessToken);
  await getManageableGroupRecord(context.client, context.user.id, groupId);

  await context.client
    .from("group_drill_safe_users")
    .delete()
    .eq("group_id", groupId);

  const { data: group, error } = await context.client
    .from("groups")
    .update({ drill_active: true, drill_started_at: new Date().toISOString() })
    .eq("id", groupId)
    .select("id, name, description, created_by, join_code, drill_active, drill_started_at")
    .single();

  if (error) throw error;
  return mapGroup(group, "admin");
}

// end a drill for a group (admin only)
async function endGroupDrill(accessToken, groupId) {
  const context = await requireAdminContext(accessToken);
  await getManageableGroupRecord(context.client, context.user.id, groupId);

  const { data: group, error } = await context.client
    .from("groups")
    .update({ drill_active: false, drill_started_at: null })
    .eq("id", groupId)
    .select("id, name, description, created_by, join_code, drill_active, drill_started_at")
    .single();

  if (error) throw error;
  return mapGroup(group, "admin");
}

// mark me safe in an active drill
async function markMemberDrillSafe(accessToken, groupId) {
  const context = await getSessionContext(accessToken);

  const { error } = await context.client
    .from("group_drill_safe_users")
    .upsert({ group_id: groupId, user_id: context.user.id }, { onConflict: "group_id,user_id", ignoreDuplicates: true });

  if (error) throw error;

  const { data: rows, error: fetchError } = await context.client
    .from("group_drill_safe_users")
    .select("user_id")
    .eq("group_id", groupId);

  if (fetchError) throw fetchError;
  return { safeUsers: (rows || []).map(r => r.user_id) };
}

// who marked safe in the active drill
async function getDrillStatus(accessToken, groupId) {
  const context = await getSessionContext(accessToken);

  const { data: rows, error } = await context.client
    .from("group_drill_safe_users")
    .select("user_id")
    .eq("group_id", groupId);

  if (error) throw error;
  return { safeUsers: (rows || []).map(r => r.user_id) };
}

module.exports = {
  createGroupForCurrentUser,
  deleteOwnedGroup,
  endGroupDrill,
  getDrillStatus,
  getManageableGroupRecord,
  getVisibleGroups,
  leaveGroup,
  markMemberDrillSafe,
  requireAdminContext,
  requestJoinByCode,
  reviewJoinRequest,
  startGroupDrill,
  updateOwnedGroup
};
