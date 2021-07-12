// Snowpack Configuration File
// See all supported options: https://www.snowpack.dev/reference/configuration

import proxy from 'http2-proxy'

/** @type {import("snowpack").SnowpackUserConfig } */
export default {
  mount: {
    static: '/',
    compiled: '/compiled'
  },
  plugins: [
    '@snowpack/plugin-postcss',
    '@snowpack/plugin-react-refresh',
  ],
  packageOptions: {
    knownEntrypoints: [
      '@microsoft/signalr'
    ]
  },
  devOptions: {
    port: 3000,
    // hmr: true,
    // hmrPort: 3001,
    // NB: don't clear shell since we are running in same process as fable
    output: 'stream',
    // NB: don't open the browser
    open: 'none',
    tailwindConfig: './tailwind.config.js',
  },
  buildOptions: {
    // NB: build directory
    out: 'out'
  },
  routes: [
    // NB: proxy hub requests to server
    {
      src: '/hub/.*|/hub\?.*',
      dest: (req, res) => proxy.web(req, res, { hostname: 'localhost', port: '5000' }),
      upgrade: (req, socket, head) => {
        const defaultWSHandler = (err, req, socket, head) => {
          if (err) {
            console.error('proxy error', err);
            socket.destroy();
          }
        }
        proxy.ws(req, socket, head, { hostname: 'localhost', port: 5000}, defaultWSHandler)
      } },
    // NB" fallback to index.html on all other routes
    { match: 'routes', src: '.*', dest: '/index.html'}
  ]
};
