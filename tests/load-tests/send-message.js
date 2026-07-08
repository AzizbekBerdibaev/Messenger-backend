import http from 'k6/http';
import { check, sleep } from 'k6';

// k6 run tests/load-tests/send-message.js

export const options = {
  stages: [
    { duration: '30s', target: 50 }, // ramp up to 50 users over 30 seconds
    { duration: '1m', target: 50 },  // stay at 50 users for 1 minute
    { duration: '30s', target: 0 },  // ramp down to 0 users
  ],
  thresholds: {
    http_req_duration: ['p(95)<500'], // 95% of requests must complete below 500ms
  },
};

const BASE_URL = 'http://localhost:5000/api';

// For this test, we assume you have pre-generated a valid JWT and a valid ChatId.
// In a real load test with dynamic users, you would hit the /auth endpoints in the setup() block
// or generate them on the fly.
const JWT_TOKEN = __ENV.JWT_TOKEN || 'eyJhbG...'; // Pass via `k6 run -e JWT_TOKEN=...`
const CHAT_ID = __ENV.CHAT_ID || '00000000-0000-0000-0000-000000000000'; 

export default function () {
  const url = `${BASE_URL}/chats/${CHAT_ID}/messages`;

  const payload = JSON.stringify({
    type: 0, // Text
    content: `Load test message from k6 at ${new Date().toISOString()}`,
    mediaUrl: null,
    replyToMessageId: null
  });

  const params = {
    headers: {
      'Content-Type': 'application/json',
      'Authorization': `Bearer ${JWT_TOKEN}`,
    },
  };

  const res = http.post(url, payload, params);

  check(res, {
    'is status 201': (r) => r.status === 201,
  });

  sleep(1); // Wait 1 second between requests per VU
}
