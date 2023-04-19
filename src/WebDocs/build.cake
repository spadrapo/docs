#addin nuget:?package=Cake.FileHelpers&version=5.0.0
#addin nuget:?package=Cake.Incubator&version=7.0.0
#addin nuget:?package=Microsoft.Extensions.DependencyInjection.Abstractions&version=2.1.1
#addin nuget:?package=Microsoft.Extensions.DependencyInjection&version=2.1.1
#addin nuget:?package=dotless.Core&version=1.6.7

var target = Argument("target", "Default");

Task("less")
    .Does(() =>
{
    //Content Base
    IEnumerable<FilePath> lessFiles = GetFiles("./styles/*.less");
    Dictionary<string,string> themes = new Dictionary<string,string>();
    //Default
    themes.Add("theme.css", FileReadText("./styles/theme.less"));
    //Themes
    System.IO.Directory.SetCurrentDirectory("./styles");
    //Build
    foreach(KeyValuePair<string,string> entry in themes){
        FileWriteText("../wwwroot/css/" + entry.Key, dotless.Core.Less.Parse(entry.Value));
    }
});


Task("Default")
    .IsDependentOn("less");

RunTarget(target);