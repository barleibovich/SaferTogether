const { getConfig } = require("./configService");
const { httpError } = require("./errors");

const GROQ_CHAT_URL = "https://api.groq.com/openai/v1/chat/completions";

// is the Groq key configured? (the AI summary feature is optional)
function isGroqConfigured() {
  return Boolean(getConfig().groqApiKey);
}

// call Groq's chat endpoint and return the text; throws httpError on failure
async function chatCompletion(messages, options = {}) {
  const { groqApiKey, groqModel } = getConfig();

  if (!groqApiKey) {
    throw httpError(500, "שירות ה-AI אינו מוגדר. הוסיפו GROQ_API_KEY אל Gateway/.env והפעילו מחדש את השרת.");
  }

  if (typeof fetch !== "function") {
    throw httpError(500, "This Node version has no global fetch (need Node 18+).");
  }

  let response;
  try {
    response = await fetch(GROQ_CHAT_URL, {
      method: "POST",
      headers: {
        Authorization: `Bearer ${groqApiKey}`,
        "Content-Type": "application/json"
      },
      body: JSON.stringify({
        model: options.model || groqModel,
        messages,
        temperature: options.temperature ?? 0.4,
        max_tokens: options.maxTokens ?? 700
      })
    });
  } catch (error) {
    throw httpError(502, `Could not reach Groq: ${error.message || error}`);
  }

  const rawText = await response.text();
  let payload = null;
  try {
    payload = rawText ? JSON.parse(rawText) : null;
  } catch {
    payload = null;
  }

  if (!response.ok) {
    const detail = payload?.error?.message || rawText || `HTTP ${response.status}`;
    throw httpError(502, `Groq request failed: ${detail}`);
  }

  const content = payload?.choices?.[0]?.message?.content;
  if (!content || !content.trim()) {
    throw httpError(502, "שירות ה-AI החזיר תשובה ריקה.");
  }

  return content.trim();
}

module.exports = {
  chatCompletion,
  isGroqConfigured
};
