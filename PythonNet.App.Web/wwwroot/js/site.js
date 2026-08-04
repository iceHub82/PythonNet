let theme = localStorage.getItem('theme')
let themeInput = $("[name='theme']")
if (theme === 'auto') {
    themeInput.val(window.matchMedia('(prefers-color-scheme: dark)').matches ? 'dark' : 'light')
} else {
    themeInput.val(theme)
}