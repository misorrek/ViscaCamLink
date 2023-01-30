function getLanguage() 
{
    if (navigator.languages != undefined)
    {
        return navigator.languages[0]; 
    }

    return navigator.language || navigator.userLanguage;
}
  
function isLanguage(languageCode) 
{
    var userLang = getLanguage().toUpperCase(); 

    return userLang.startsWith(languageCode);
}