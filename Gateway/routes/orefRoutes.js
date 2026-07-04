const {
  getAccessTokenFromRequest,
  sendJson,
  sendRouteError
} = require("../http");
const { getVisibleGroups } = require("../../Services/groupService");
const {
  annotateMembersWithOrefAlerts,
  getCurrentOrefAlerts
} = require("../../Services/orefAlertService");

// live home front command status for one group we can see
async function getGroupOrefStatus(accessToken, groupId) {
  const groups = await getVisibleGroups(accessToken);
  const group = groups.find(item => item.id === groupId);

  if (!group) {
    const error = new Error("Group not found");
    error.statusCode = 404;
    throw error;
  }

  const alerts = await getCurrentOrefAlerts();
  const members = annotateMembersWithOrefAlerts(group.members || [], alerts);
  const affectedAreas = [...new Set(alerts.flatMap(alert => alert.areas))];

  return {
    affectedAreas,
    alerts,
    fetchedAt: new Date().toISOString(),
    groupId: group.id,
    groupName: group.name,
    hasActiveAlert: alerts.length > 0,
    hasGroupAlert: members.some(member => member.status === "alert"),
    members,
    source: "pikud_haoref"
  };
}

// handle the oref alert routes
async function handleOrefRoute(request, response, pathname) {
  const groupStatusMatch = pathname.match(/^\/api\/oref\/groups\/([^/]+)\/status$/);

  try {
    if (groupStatusMatch && request.method === "GET") {
      const accessToken = getAccessTokenFromRequest(request);
      const status = await getGroupOrefStatus(accessToken, groupStatusMatch[1]);

      sendJson(response, 200, status, {
        "Cache-Control": "no-store"
      });
      return true;
    }
  } catch (error) {
    sendRouteError(response, error);
    return true;
  }

  return false;
}

module.exports = {
  getGroupOrefStatus,
  handleOrefRoute
};
