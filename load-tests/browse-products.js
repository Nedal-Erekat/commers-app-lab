import http from 'k6/http';
import { check, sleep } from 'k6';

// Override with: k6 run -e BASE_URL=https://<host> browse-products.js
const BASE_URL = __ENV.BASE_URL || 'http://localhost:5000';

export const options = {
  scenarios: {
    browse: {
      executor: 'ramping-vus',
      stages: [
        { duration: '20s', target: 50 },   // ramp up to 50 VUs
        { duration: '1m', target: 100 },   // sustain at 100 VUs
        { duration: '20s', target: 0 },    // ramp down
      ],
    },
  },
  thresholds: {
    http_req_duration: ['p(95)<500'],  // 95th percentile under 500ms
    http_req_failed: ['rate<0.01'],    // error rate under 1%
  },
};

export default function () {
  const res = http.get(`${BASE_URL}/api/products?page=1&pageSize=50`);

  check(res, {
    'status is 200': (r) => r.status === 200,
    'served-by present': (r) => r.headers['X-Served-By'] !== undefined,
  });

  sleep(1);
}
