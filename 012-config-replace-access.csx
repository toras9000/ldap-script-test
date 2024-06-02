#r "nuget: Lestaly, 0.58.0"
#r "nuget: Kokuban, 0.2.0"
#load ".directory-service-extensions.csx"
#load ".text-helper.csx"
#nullable enable
using System.DirectoryServices.Protocols;
using System.Net;
using Kokuban;
using Lestaly;

var settings = new
{
    // LDAP server settings
    Server = new
    {
        // Host name or ip
        Host = "localhost",

        // Port number
        Port = 1389,

        // Use SSL
        Ssl = false,

        // LDAP protocol version
        ProtocolVersion = 3,

        // Bind user credential, null is anonymous
        BindCredential = new NetworkCredential("cn=config-admin,cn=config", "config-admin-pass"),

        // Configuration Base DN
        ConfigDn = "olcDatabase={2}mdb,cn=config",

        // Access definitions to be added
        AccessDefineFile = ThisSource.RelativeFile("010-config-access-data.txt"),
    },

};

return await Paved.RunAsync(config: o => o.AnyPause(), action: async () =>
{
    // Read the access definition to be added
    var accessDefines = settings.Server.AccessDefineFile.EnumerateTextBlocks().ToArray();

    // Bind to LDAP server
    WriteLine("Bind to LDAP server");
    var server = new LdapDirectoryIdentifier(settings.Server.Host, settings.Server.Port);
    using var ldap = new LdapConnection(server);
    ldap.SessionOptions.SecureSocketLayer = settings.Server.Ssl;
    ldap.SessionOptions.ProtocolVersion = settings.Server.ProtocolVersion;
    ldap.AuthType = AuthType.Basic;
    ldap.Credential = settings.Server.BindCredential;
    ldap.Bind();

    // Create a search request.
    WriteLine("Request a search");
    var searchReq = new SearchRequest();
    searchReq.DistinguishedName = settings.Server.ConfigDn;
    searchReq.Scope = SearchScope.Base;

    // Request a search.
    var searchRsp = await ldap.SendRequestAsync(searchReq);
    if (searchRsp.ResultCode != 0) throw new PavedMessageException($"failed to search: {searchRsp.ErrorMessage}");
    var searchResult = searchRsp as SearchResponse ?? throw new PavedMessageException("unexpected result");

    // Read the existing access definition.
    var configEntry = searchResult.Entries[0];
    var accessExists = configEntry.EnumerateAttributeValues("olcAccess").ToArray();

    // Remove all existing access.
    if (0 < accessExists.Length)
    {
        WriteLine("Delete all access");
        var attrModify = new DirectoryAttributeModification();
        attrModify.Operation = DirectoryAttributeOperation.Delete;
        attrModify.Name = "olcAccess";
        foreach (var access in accessDefines)
        {
            attrModify.Add(access);
        }

        var accessDelete = new ModifyRequest();
        accessDelete.DistinguishedName = settings.Server.ConfigDn;
        accessDelete.Modifications.Add(attrModify);
        var deleteRsp = await ldap.SendRequestAsync(accessDelete);
        if (deleteRsp.ResultCode != 0) throw new PavedMessageException($"failed to modify: {deleteRsp.ErrorMessage}");
    }

    // Add defined access.
    WriteLine("Request to add access.");
    {
        var attrModify = new DirectoryAttributeModification();
        attrModify.Operation = DirectoryAttributeOperation.Add;
        attrModify.Name = "olcAccess";
        foreach (var access in accessDefines)
        {
            attrModify.Add(access);
        }

        var accessAdd = new ModifyRequest();
        accessAdd.DistinguishedName = settings.Server.ConfigDn;
        accessAdd.Modifications.Add(attrModify);
        var modifyRsp = await ldap.SendRequestAsync(accessAdd);
        if (modifyRsp.ResultCode != 0) throw new PavedMessageException($"failed to modify: {modifyRsp.ErrorMessage}");
    }

    WriteLine("Completed.");
});
