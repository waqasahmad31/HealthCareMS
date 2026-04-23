import http from "k6/http";
import { check, sleep } from "k6";

export const options = {
  scenarios: {
    three_g_online_consultation: {
      executor: "constant-vus",
      vus: 3,
      duration: "2m",
    },
  },
  thresholds: {
    http_req_failed: ["rate<0.01"],
    http_req_duration: ["p(95)<3000", "p(99)<5000"],
  },
};

const baseUrl = __ENV.BASE_URL || "http://localhost:5270";
const token = __ENV.ACCESS_TOKEN || "";
const sessionId = __ENV.SESSION_ID || "";

export default function () {
  const headers = token ? { Authorization: `Bearer ${token}` } : {};

  const ping = http.get(`${baseUrl}/api/v1/system/ping`);
  check(ping, { "ping 200": (response) => response.status === 200 });

  if (token && sessionId) {
    const session = http.get(`${baseUrl}/api/v1/consultations/sessions/${sessionId}`, { headers });
    check(session, { "session 200": (response) => response.status === 200 });

    const chat = http.get(`${baseUrl}/api/v1/consultations/sessions/${sessionId}/chat/messages`, { headers });
    check(chat, { "chat 200": (response) => response.status === 200 });
  }

  sleep(3);
}
