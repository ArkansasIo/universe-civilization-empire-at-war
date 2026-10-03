module.exports = {
  apps: [
    {
      name: 'bsat-typescript-server',
      script: 'dist-server/index.js',
      instances: 1,
      autorestart: true,
      watch: ['dist-server'],
      max_memory_restart: '500M',
      env: {
        NODE_ENV: 'production',
        PORT: 3000,
      },
      env_development: {
        NODE_ENV: 'development',
        PORT: 3000,
      },
    },
  ],
};
