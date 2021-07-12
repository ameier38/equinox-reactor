module.exports = {
  mode: 'jit',
  purge: ['./static/**/*.html', './compiled/**/*.js'],
  plugins: [
    require('@tailwindcss/forms')
  ]
  // specify other options here
};
