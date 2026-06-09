const {
  getAccessTokenFromRequest,
  readJsonBody,
  sendJson,
  sendRouteError
} = require("../http");
const {
  activateGroupActivity,
  createGroupActivity,
  deactivateGroupActivity,
  deleteGroupActivity,
  getActiveGroupActivities,
  getGroupActivities,
  getGroupActivityResults,
  reviewGroupActivityResult,
  submitGroupActivityResult
} = require("../../Services/activityService");

// handles the activity routes: create/activate + results
async function handleActivityRoute(request, response, pathname, requestUrl) {
  const accessToken = getAccessTokenFromRequest(request);
  const activitiesMatch = pathname.match(/^\/api\/groups\/([^/]+)\/activities$/);
  const activityMatch = pathname.match(/^\/api\/groups\/([^/]+)\/activities\/([^/]+)$/);
  const activityActivationMatch = pathname.match(/^\/api\/groups\/([^/]+)\/activities\/([^/]+)\/activate$/);
  const activeActivitiesMatch = pathname.match(/^\/api\/groups\/([^/]+)\/active-activities$/);
  const activityModeMatch = pathname.match(/^\/api\/groups\/([^/]+)\/activity-activations\/([^/]+)\/([^/]+)$/);
  const resultsMatch = pathname.match(/^\/api\/groups\/([^/]+)\/activity-results$/);
  const resultMatch = pathname.match(/^\/api\/groups\/([^/]+)\/activity-results\/([^/]+)$/);

  try {
    if (activitiesMatch && request.method === "GET") {
      const activities = await getGroupActivities(accessToken, activitiesMatch[1]);
      sendJson(response, 200, { activities });
      return true;
    }

    if (activitiesMatch && request.method === "POST") {
      const body = await readJsonBody(request);
      const activity = await createGroupActivity(accessToken, activitiesMatch[1], body);
      sendJson(response, 201, { activity });
      return true;
    }

    if (activityMatch && request.method === "DELETE") {
      const result = await deleteGroupActivity(accessToken, activityMatch[1], activityMatch[2]);
      sendJson(response, 200, result);
      return true;
    }

    if (activityActivationMatch && request.method === "POST") {
      const body = await readJsonBody(request);
      const activation = await activateGroupActivity(
        accessToken,
        activityActivationMatch[1],
        activityActivationMatch[2],
        body
      );
      sendJson(response, 200, activation);
      return true;
    }

    if (activityModeMatch && request.method === "DELETE") {
      const result = await deactivateGroupActivity(
        accessToken,
        activityModeMatch[1],
        activityModeMatch[3],
        activityModeMatch[2]
      );
      sendJson(response, 200, result);
      return true;
    }

    if (activeActivitiesMatch && request.method === "GET") {
      const mode = requestUrl.searchParams.get("mode") || "";
      const activities = await getActiveGroupActivities(accessToken, activeActivitiesMatch[1], mode);
      sendJson(response, 200, { activities });
      return true;
    }

    if (resultsMatch && request.method === "GET") {
      const results = await getGroupActivityResults(accessToken, resultsMatch[1]);
      sendJson(response, 200, { results });
      return true;
    }

    if (resultsMatch && request.method === "POST") {
      const body = await readJsonBody(request);
      const result = await submitGroupActivityResult(accessToken, resultsMatch[1], body);
      sendJson(response, 201, { result });
      return true;
    }

    if (resultMatch && request.method === "PATCH") {
      const body = await readJsonBody(request);
      const result = await reviewGroupActivityResult(
        accessToken,
        resultMatch[1],
        resultMatch[2],
        body
      );
      sendJson(response, 200, { result });
      return true;
    }
  } catch (error) {
    sendRouteError(response, error);
    return true;
  }

  return false;
}

module.exports = {
  handleActivityRoute
};
