module.exports = {
  mode: 'jit',
  purge: ['./static/**/*.html', './compiled/**/*.js'],
  plugins: [
    require('@tailwindcss/forms')
  ],
  variants: {
    extend: {
      opacity: ['disabled'],
    }
  }
};
