{
    document.getElementById('icon1').innerHTML = loadSvgContent('App_Data/funnel.svg');
    document.getElementById('icon2').innerHTML = loadSvgContent('App_Data/other-icon.svg');

    function loadSvgContent(path) {
        var xhr = new XMLHttpRequest();
        xhr.open('GET', path, false);
        xhr.send();
        return xhr.responseText;
    }
  "exclude": [
    "**/bin",
    "**/bower_components",
    "**/jspm_packages",
    "**/node_modules",
    "**/obj",
    "**/platforms"
  ]
}