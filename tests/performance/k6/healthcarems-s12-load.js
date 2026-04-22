import http from "k6/http";
import { check, sleep } from "k6";

export const options = {
  stages: [
    { duration: "30s", target: 100 },
    { duration: "1m", target: 500 },
    { duration: "2m", target: 500 },
    { duration: "30s", target: 0 },
  ],
  thresholds: {
    http_req_failed: ["rate<0.01"],
    http_req_duration: ["p(95)<750", "p(99)<1500"],
  },
};

const baseUrl = __ENV.BASE_URL || "http://localhost:5270";
const token = __ENV.ACCESS_TOKEN || "";

export default function () {
  const headers = token ? { Authorization: `Bearer ${token}` } : {};

  const ping = http.get(`${baseUrl}/api/v1/system/ping`);
  check(ping, {
    "ping 200": (response) => response.status === 200,
  });

  if (token) {
    const appointments = http.get(
      `${baseUrl}/api/v1/admin/appointments/overview?pageNumber=1&pageSize=50`,
      { headers },
    );
    check(appointments, {
      "appointments overview 200": (response) => response.status === 200,
    });

    const doctors = http.get(`${baseUrl}/api/v1/admin/doctors/management`, { headers });
    check(doctors, {
      "doctor management 200": (response) => response.status === 200,
    });
  }

  sleep(1);
}
