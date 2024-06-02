#r "nuget: Lestaly, 0.58.0"
#nullable enable
using Lestaly;
using Lestaly.Cx;

return await Paved.RunAsync(async () =>
{
    var composeFile = ThisSource.RelativeFile("compose.yml");
    await "docker".args("compose", "--file", composeFile.FullName, "down", "--remove-orphans").silent();
    ThisSource.RelativeDirectory("volumes").DeleteRecurse();
});
