import http from 'k6/http';
import { check, sleep } from 'k6';

const BASE_URL = __ENV.BASE_URL || 'http://localhost:5000';

// Varied terms — wide enough to defeat any accidental in-memory caching
const SEARCH_TERMS = [
  'Phone', 'Camera', 'Chair', 'Bottle', 'Lamp',
  'Bike', 'Keyboard', 'Bag', 'Shirt', 'Table',
  'Sleek', 'Refined', 'Handcrafted', 'Ergonomic', 'Small',
  'Practical', 'Intelligent', 'Generic', 'Unbranded', 'Licensed',
];

export const options = {
  scenarios: {
    search_load: {
      executor: 'ramping-vus',
      stages: [
        { duration: '30s', target: 50 },  // ramp up
        { duration: '1m', target: 50 },   // sustained load
        { duration: '10s', target: 0 },   // ramp down
      ],
    },
  },
  thresholds: {
    'http_req_duration{endpoint:search}': ['p(95)<2000'],
    http_req_failed: ['rate<0.01'],
  },
};

export default function () {
  const term = SEARCH_TERMS[Math.floor(Math.random() * SEARCH_TERMS.length)];

  const res = http.get(
    `${BASE_URL}/api/products/search?q=${term}`,
    { tags: { endpoint: 'search' } }
  );

  check(res, { 'status is 200': (r) => r.status === 200 });

  sleep(1);
}
