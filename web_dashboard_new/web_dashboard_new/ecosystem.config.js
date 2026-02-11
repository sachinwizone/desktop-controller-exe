module.exports = {
  apps: [{
    name: 'desktop-controller-web',
    script: 'server.js',
    cwd: '/root/desktop-controller-exe/web_dashboard_new/web_dashboard_new',
    instances: 1,
    autorestart: true,
    watch: false,
    max_memory_restart: '1G',
    env: {
      NODE_ENV: 'production',
      PORT: 8888
    },
    error_file: '/root/logs/web-error.log',
    out_file: '/root/logs/web-out.log',
    log_file: '/root/logs/web-combined.log',
    time: true
  }]
};
