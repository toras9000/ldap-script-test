#r "nuget: Lestaly, 0.58.0"
#nullable enable
using Lestaly;
using Lestaly.Cx;

return await Paved.RunAsync(async () =>
{
    var composeFile = ThisSource.RelativeFile("docker-compose.yml");
    await "docker".args("compose", "--file", composeFile.FullName, "down", "--remove-orphans").silent();
    ThisSource.RelativeDirectory("volumes").DeleteRecurse();
    await "docker".args("compose", "--file", composeFile.FullName, "up", "-d").silent().result().success();
});
