const { handleRequest } = require("../Gateway/server");

module.exports = function saferTogetherApi(request, response) {
  return handleRequest(request, response);
};
