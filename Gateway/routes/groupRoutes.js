const {
  getAccessTokenFromRequest,
  readJsonBody,
  sendJson,
  sendRouteError
} = require("../http");
const {
  createGroupForCurrentUser,
  deleteOwnedGroup,
  endGroupDrill,
  getVisibleGroups,
  leaveGroup,
  requestJoinByCode,
  reviewJoinRequest,
  startGroupDrill,
  updateOwnedGroup
} = require("../../Services/groupService");

// This function handles all the group routes.
async function handleGroupRoute(request, response, pathname) {
  const accessToken = getAccessTokenFromRequest(request);
  const groupMatch = pathname.match(/^\/api\/groups\/([^/]+)$/);
  const drillMatch = pathname.match(/^\/api\/groups\/([^/]+)\/drill$/);
  const joinRequestMatch = pathname.match(/^\/api\/groups\/([^/]+)\/join-requests\/([^/]+)$/);
  const leaveMemberMatch = pathname.match(/^\/api\/groups\/([^/]+)\/members\/me$/);

  try {
    if (pathname === "/api/groups" && request.method === "GET") {
      const groups = await getVisibleGroups(accessToken);
      sendJson(response, 200, { groups });
      return true;
    }

    if (pathname === "/api/groups" && request.method === "POST") {
      const body = await readJsonBody(request);
      const group = await createGroupForCurrentUser(accessToken, body);
      sendJson(response, 201, { group });
      return true;
    }

    if (pathname === "/api/groups/join-requests" && request.method === "POST") {
      const body = await readJsonBody(request);
      const joinRequest = await requestJoinByCode(accessToken, body);
      sendJson(response, 201, { joinRequest });
      return true;
    }

    if (joinRequestMatch && request.method === "PATCH") {
      const body = await readJsonBody(request);
      const joinRequest = await reviewJoinRequest(
        accessToken,
        joinRequestMatch[1],
        joinRequestMatch[2],
        body
      );
      sendJson(response, 200, { joinRequest });
      return true;
    }

    if (groupMatch && request.method === "PATCH") {
      const body = await readJsonBody(request);
      const group = await updateOwnedGroup(accessToken, groupMatch[1], body);
      sendJson(response, 200, { group });
      return true;
    }

    if (leaveMemberMatch && request.method === "DELETE") {
      const result = await leaveGroup(accessToken, leaveMemberMatch[1]);
      sendJson(response, 200, result);
      return true;
    }

    if (groupMatch && request.method === "DELETE") {
      const result = await deleteOwnedGroup(accessToken, groupMatch[1]);
      sendJson(response, 200, result);
      return true;
    }

    if (drillMatch && request.method === "POST") {
      const group = await startGroupDrill(accessToken, drillMatch[1]);
      sendJson(response, 200, { group });
      return true;
    }

    if (drillMatch && request.method === "DELETE") {
      const group = await endGroupDrill(accessToken, drillMatch[1]);
      sendJson(response, 200, { group });
      return true;
    }
  } catch (error) {
    sendRouteError(response, error);
    return true;
  }

  return false;
}

module.exports = {
  handleGroupRoute
};
