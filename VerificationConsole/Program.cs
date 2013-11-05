using ConsoleTools;
using SheerID;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VerificationConsole
{
    class Program
    {
        static Output output = new Output(typeof(SheerID.API).Assembly);
        static void Main(string[] args)
        {

            var accessCode = "";
            var keypath = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile) + "\\accesscode";
            if (System.IO.File.Exists(keypath))
            {
                accessCode = System.IO.File.ReadAllText(keypath);
            }
            while (accessCode == "")
            {
                Console.Write("Enter your access code: ");
                accessCode = Console.ReadLine();
                var r = new System.Text.RegularExpressions.Regex("[a-zA-Z0-9]*"); //block unwanted characters
                accessCode = r.Match(accessCode).Value;
            }
            var sheerid = new SheerID.API(accessCode);
            if (sheerid.Ping())
            {
                while (1 == 1)
                {
                    Console.Clear();
                    var choices = new[] { "Verify", "Namespace Management", "Notifier Management", "Quit" };
                    var choice = output.PromptKeyMatrix<string>(choices, "Main Menu:");
                    if (choice == choices[0]) { VerifyConsole(sheerid); }
                    else if (choice == choices[1]) { NamespaceConsole(sheerid); }
                    else if (choice == choices[2]) { new NotifierConsole().Execute(sheerid); }
                    else if (choice == choices[3]) { break; }
                }

            }
            else
            {
                Console.WriteLine("SheerID service is inaccessible.");
                Console.ReadKey();
            }
            
        }
        private interface IConsoleCommand
        {
            void Execute(SheerID.API sheerid);
        }
        private class NotifierConsole : IConsoleCommand
        {
            public void Execute(SheerID.API sheerid)
            {
                while (1 == 1)
                {
                    Console.Clear();
                    var choices = new[] { "Add", "Fire", "Delete", "Main Menu" };
                    var choice = output.PromptKeyMatrix<string>(choices, "Email Notifier Management:");
                    if (choice == choices[0]) { new AddEmailNotifierConsole().Execute(sheerid); }
                    if (choice == choices[2]) { new DeleteEmailNotifierConsole().Execute(sheerid); }
                    if (choice == choices[3]) { break; }
                }
            }

            private class AddEmailNotifierConsole : IConsoleCommand
            {
                public void Execute(SheerID.API sheerid)
                {
                    Console.Clear();

                    bool alwaysEmailOnSuccess;
                    while (1 == 1)
                    {
                        Console.Clear();
                        var choices = new[] { "Yes", "No" };
                        var choice = output.PromptKeyMatrix<string>(choices, "Always Email On Success?", defaultChoice: 0).First(o => o.Key.Selected == true).Value;
                        alwaysEmailOnSuccess = (choice == choices[0]);
                        break;
                    }

                    var promptForValues = new Dictionary<string, ConsoleNameValue>()
                    {
                        { "emailFromAddress", new ConsoleNameValue() { DisplayName = "Email From Address"}},
                        { "emailFromName", new ConsoleNameValue() {DisplayName = "Email From Name"}},
                        { "successEmailSubject", new ConsoleNameValue() {DisplayName = "Success Email Subject"}},
                        { "successEmail", new ConsoleNameValue() {DisplayName = "Success Email"}},
                        { "failureEmailSubject", new ConsoleNameValue() {DisplayName = "Failure Email Subject"}},
                        { "failureEmail", new ConsoleNameValue() {DisplayName = "Failure Email"}}
                    };

                    foreach (var nameValue in promptForValues)
                    {
                        string newValue = "";
                        while (newValue == "")
                        {
                            Console.Write(nameValue.Value.DisplayName + ": ");
                            newValue = Console.ReadLine();
                        }
                        nameValue.Value.Value = newValue;
                    }

                    //TODO: Prompt for filters

                    var tags = new List<string>();
                    Console.WriteLine("Enter one or more tags. Hit enter on an empty line to continue.");
                    while (1 == 1)
                    {
                        Console.Write("New Tag: ");
                        var newTag = Console.ReadLine();
                        if (newTag != "")
                        {
                            tags.Add(newTag);
                        }
                        else if (tags.Count() > 0)
                        {
                            break;
                        }
                    }

                    using (var response = sheerid.AddNotifier(
                        type: NotifierType.EMAIL,
                        emailFromAddress: promptForValues["emailFromAddress"].Value,
                        emailFromName: promptForValues["emailFromName"].Value,
                        successEmailSubject: promptForValues["successEmailSubject"].Value,
                        successEmail: promptForValues["successEmail"].Value,
                        failureEmailSubject: promptForValues["failureEmailSubject"].Value,
                        failureEmail: promptForValues["failureEmail"].Value,
                        alwaysEmailOnSuccess: alwaysEmailOnSuccess,
                        filters: null,
                        tags: tags
                        ))
                    {
                        if (IsError(response)) { return; }
                        Console.WriteLine("New Notifier Added:");
                        output.OutputObject(response.GetResponse());
                        Console.ReadKey();
                    }
                }
            }
            private class DeleteEmailNotifierConsole : IConsoleCommand
            {
                public void Execute(API sheerid)
                {
                    Console.Clear();
                    using (var serviceResponse = sheerid.ListNotifiers())
                    {
                        if (IsError(serviceResponse)) { return; }
                        var response = serviceResponse.GetResponse();
                        if (IsZeroCount(response)) { return; }
                        foreach (var selection in output.PromptKeyMatrix<SheerID.API.Notifier>(response, "Select Notifiers to Delete", Output.MatrixSelectionMethod.SelectMany))
                        {
                            if (sheerid.DeleteNotifier(selection.Value.Id))
                            {
                                Console.WriteLine("Delete Notifier [" + selection.Value.Id + "] Succeeded");
                            }
                            else
                            {
                                Console.WriteLine("Delete Notifier [" + selection.Value.Id + "] Failed");
                            }
                            Console.ReadKey();
                        }
                    }
                }
            }
            public class FireEmailNotifierConsole : IConsoleCommand
            {
                public string requestId = "";
                public void Execute(API sheerid)
                {
                    Console.Clear();
                    using (var serviceResponse = sheerid.ListNotifiers())
                    {
                        if (IsError(serviceResponse)) { return; }
                        var response = serviceResponse.GetResponse();
                        if (IsZeroCount(response)) { return; }
                        var notifier = output.PromptKeyMatrix<SheerID.API.Notifier>(response, "Select Notifiers to Fire");
                        if (requestId == "")
                        {
                            Console.Write("RequestId: ");
                            requestId = Console.ReadLine();
                        }
                        if (sheerid.FireNotifier(notifier.Id ,requestId, NotifierEventType.CREATE))
                        {
                            Console.WriteLine("Notifier [" + notifier.ToString() + "] Succeeded");
                        }
                        else
                        {
                            Console.WriteLine("Notifier [" + notifier.ToString() + "] Failed");
                        }
                        Console.ReadKey();
                    }
                }
            }

        }

        //TODO: Convert Console methods to IConsoleCommands
        static void NamespaceConsole(SheerID.API sheerid)
        {
            while (1 == 1)
            {
                Console.Clear();
                var choices = new[] { "List", "Map", "Delete", "Main Menu" };
                var choice = output.PromptKeyMatrix<string>(choices, "Namespace Management:");
                if (choice == choices[0]) { VerifyListConsole(sheerid); }
                if (choice == choices[1]) { NamespaceMapConsole(sheerid); }
                if (choice == choices[2]) { NamespaceDeleteConsole(sheerid); }
                if (choice == choices[3]) { break; }
            }
        }

        static void VerifyListConsole(SheerID.API sheerid)
        {
            using (var serviceResponse = sheerid.ListNamespaces())
            {
                if (IsError(serviceResponse)) { return; }
                var response = serviceResponse.GetResponse();
                if (IsZeroCount(response)) { return; }
                output.OutputObject(response);
                Console.ReadKey();
            }
        }

        static void NamespaceMapConsole(SheerID.API sheerid)
        {
            Console.Clear();
            using (var serviceResponse = sheerid.ListTemplates())
            {
                if (IsError(serviceResponse)) { return; }
                var response = serviceResponse.GetResponse();
                if (IsZeroCount(response)) { return; }
                var template = output.PromptKeyMatrix<API.VerificationRequestTemplate>(response, "Select a template");
                while (1 == 1)
                {
                    var namespaceName = output.PromptFor<string>("Enter a namespace, must contain only lowercase, '-' and numeric characters.");
                    if (namespaceName == "")
                    {
                        Console.WriteLine("Canceled.");
                        Console.ReadKey();
                        break;
                    }
                    else if (!API.Namespace.IsValidateName(namespaceName))
                    {
                        continue;
                    }
                    else
                    {
                        using (var namespaceServiceResponse = sheerid.MapNamespace(namespaceName, template))
                        {
                            if (IsError(namespaceServiceResponse)) { return; }
                            var newNamespace = namespaceServiceResponse.GetResponse();
                            Console.WriteLine("New namespace created:");
                            output.OutputObject(newNamespace);
                            Console.ReadKey();
                            break;
                        }
                    }

                }
            }
        }

        static void NamespaceDeleteConsole(SheerID.API sheerid)
        {
            Console.Clear();
            using (var serviceResponse = sheerid.ListNamespaces())
            {
                if (IsError(serviceResponse)) { return; }
                var response = serviceResponse.GetResponse();
                if (IsZeroCount(response)) { return; }
                foreach (var selection in output.PromptKeyMatrix<SheerID.API.Namespace>(serviceResponse.GetResponse(), "Select Namespaces to Delete", Output.MatrixSelectionMethod.SelectMany))
                {
                    sheerid.DeleteNamespace(selection.Value.Name);
                }
            }
        }

        static void VerifyConsole(SheerID.API sheerid)
        {
            var verification = sheerid.Verification;

            Console.Clear();
            using (var serviceResponse = sheerid.ListOrganizationTypes())
            {
                if (IsError(serviceResponse)) { return; }
                verification.organizationType = output.PromptKeyMatrix<OrganizationType>(serviceResponse.GetResponse(), "Organization Types");
            }

            var organizationNameFilter = output.PromptFor<string>("Enter an organization search filter or hit enter for the complete list.");

            Console.Clear();

            using (var serviceResponse = sheerid.ListOrganizations(verification.organizationType, organizationNameFilter))
            {
                if (IsError(serviceResponse)) { return; }
                var organizations = serviceResponse.GetResponse();
                if (IsZeroCount(organizations)) { return; }
                var organization = output.PromptKeyMatrix<SheerID.API.Organization>(serviceResponse.GetResponse(), "Organizations");
                verification.organizationId = organization.ID;
                verification.organizationName = organization.Name;
            }

            Console.Clear();
            using (var serviceResponse = sheerid.ListAffiliationTypes(verification.organizationType))
            {
                if (IsError(serviceResponse)) { return; }
                foreach (var selection in output.PromptKeyMatrix<SheerID.AffiliationType>(serviceResponse.GetResponse(), "Affiliation Types", Output.MatrixSelectionMethod.SelectMany))
                    verification.affiliationTypes.Add(selection.Value);
            }

            Console.Clear();
            var infoCollection = verification.PersonalInfo.GetType().GetFields().Where(o => sheerid.GetFields(verification.affiliationTypes).GetResponse().Contains(o.Name)).ToArray();
            var infoMatrix = output.CreateKeyMatrix<System.Reflection.FieldInfo>(infoCollection);
            var infoType = verification.PersonalInfo.GetType();
            var infoRenameKey = new Dictionary<string, string>()
                    {
                        {"EMAIL", "Email"},
                        {"FIRST_NAME", "First Name"},
                        {"MIDDLE_NAME", "Middle Name"},
                        {"LAST_NAME", "Last Name"},
                        {"FULL_NAME", "Full Name"},
                        {"COMPANY_NAME", "Company Name"},
                        {"BIRTH_DATE", "Birth Date [ex. 1996-02-28]"},
                        {"ID_NUMBER", "ID Number"},
                        {"JOB_TITLE", "Job Title"},
                        {"USERNAME", "Username"},
                        {"POSTAL_CODE", "Postal Code"},
                        {"PHONE_NUMBER", "Phone Number"},
                        {"SSN", "SSN"},
                        {"SSN_LAST4", "SSN Last 4"},
                        {"STATUS_START_DATE", "Status Start Date"},
                        {"SUFFIX", "Suffix"},
                        {"RELATIONSHIP", "Relationship"}
                    };

            foreach (var f in infoMatrix)
                if (infoRenameKey.ContainsKey(f.Value.Name))
                    f.Key.DisplayName = infoRenameKey[f.Value.Name];

            while (1 == 1)
            {
                if (verification.requestId != null)
                {
                    Console.Clear();
                    var choices = new[] { "Inquire on status", "Update personal information", "Upload assets", "List assets", "Post a new verification", "Download asset to desktop", "Fire Notifier" };
                    var choice = output.PromptKeyMatrix<string>(choices, "Request exists:");
                    if (choice == choices[0])//inquire
                    {
                        using (var serviceResponse = sheerid.Inquire(verification.requestId))
                        {
                            if (IsError(serviceResponse)) { continue; }
                            output.OutputObject(serviceResponse.GetResponse());
                        }
                        Console.ReadKey();
                        continue; //skip past verification post
                    }
                    else if (choice == choices[1])//update
                    {
                        //do nothing
                    }
                    else if (choice == choices[2])//upload
                    {
                        Console.Clear();
                        var files = new List<SheerID.API.UploadableFile>();
                        using (var serviceResponse = sheerid.ListAssetTypes(verification.organizationType))
                        {
                            if (IsError(serviceResponse)) { return; }
                            foreach (var selection in output.PromptKeyMatrix<SheerID.AssetType>(serviceResponse.GetResponse(), "Asset Types", Output.MatrixSelectionMethod.SelectMany))
                            {
                                verification.assetTypes.Add(selection.Value);
                                Console.Write(selection.Value.ToString() + " [TestImage.png]>");
                                var filePath = Console.ReadLine();

                                var file = new SheerID.API.UploadableFile();
                                if (filePath == "")
                                {
                                    file.fileContentType = "image/png";
                                    file.fileName = "TestImage.png";
                                    var stream = System.Reflection.Assembly.GetEntryAssembly().GetManifestResourceStream("VerificationConsole.TestImage.png");
                                    file.data = new byte[stream.Length];
                                    stream.Read(file.data, 0, (int)stream.Length);
                                }
                                else
                                {
                                    file.fileContentType = System.Web.MimeMapping.GetMimeMapping(filePath);
                                    file.fileName = System.IO.Path.GetFileName(filePath);
                                    file.data = System.IO.File.ReadAllBytes(filePath);
                                }
                                files.Add(file);
                            }
                        }
                        var assetToken = sheerid.GetAssetToken(verification.requestId).GetResponse();

                        Console.WriteLine("Upload Response:");
                        output.OutputObject(sheerid.PostAsset(assetToken, files).GetResponse(), depth: 1);
                        Console.ReadKey();
                        continue; //skip past verification post
                    }
                    if (choice == choices[3])//list assets
                    {
                        using (var serviceResponse = sheerid.ListAssets(verification.requestId))
                        {
                            if (IsError(serviceResponse)) { continue; }
                            output.OutputObject(serviceResponse.GetResponse());
                        }
                        Console.ReadKey();
                        continue; //skip past verification post
                    }
                    else if (choice == choices[4])//new
                    {
                        verification.requestId = null;
                    }
                    else if (choice == choices[5])//download
                    {
                        Console.Write("AssetID: ");
                        Console.Clear();
                        using (var serviceResponse = sheerid.ListAssets(verification.requestId))
                        {
                            if (IsError(serviceResponse)) { continue; }
                            var assets = serviceResponse.GetResponse();
                            if (IsZeroCount(assets)) { continue; }
                            foreach (var selection in output.PromptKeyMatrix<SheerID.API.Asset>(serviceResponse.GetResponse(), "Assets", Output.MatrixSelectionMethod.SelectMany))
                            {
                                Console.WriteLine(selection.Value.Name + " download started...");
                                using (var dataResponse = sheerid.GetAssetData(selection.Value.ID))
                                {
                                    if (IsError(dataResponse)) { continue; }
                                    var fileData = dataResponse.GetResponse();
                                    var path = output.GetUniqueFilePath(System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), selection.Value.Name));
                                    System.IO.File.WriteAllBytes(path, fileData);
                                    Console.WriteLine("Saved: " + path);
                                }
                            }
                        }
                        Console.ReadKey();
                        continue;
                    }
                    else if (choice == choices[6])
                    {
                        (new NotifierConsole.FireEmailNotifierConsole() {requestId=verification.requestId}).Execute(sheerid);
                        continue; //skip past verification post
                    }
                }

                Console.Clear();
                foreach (var selection in output.PromptKeyMatrix<System.Reflection.FieldInfo>(infoMatrix, infoCollection, "Personal Info", Output.MatrixSelectionMethod.SelectMany))
                {
                    object oldValue = selection.Value.GetValue(verification.PersonalInfo);
                    Console.Write(infoMatrix.First(o => o.Value == infoType.GetField(selection.Value.Name)).Key.DisplayName + (oldValue != null ? " [" + oldValue + "]" : "") + ": ");
                    string newValue = Console.ReadLine();
                    selection.Value.SetValue(verification.PersonalInfo, newValue != "" ? newValue : oldValue);
                }

                Console.Clear();
                Console.WriteLine("Verification Response:");
                using (var verificationServiceResponse = verification.PostRequest())
                {
                    if (IsError(verificationServiceResponse)) { continue; }
                    var verificationResponse = verificationServiceResponse.GetResponse();
                    output.OutputObject(verificationResponse, depth: 1);

                    if (verificationServiceResponse.Status == System.Net.HttpStatusCode.OK)
                    {
                        verification.requestId = verificationResponse.RequestId; //if this object is posted again it will be an update
                        Console.WriteLine("Status: " + verificationResponse.Status + " Result: " + verificationResponse.Result);
                        foreach (var error in verificationResponse.Errors)
                        {
                            Console.WriteLine(error.Code + " " + error.Message);
                        }
                    }
                    Console.ReadKey();
                }
            }
        }

        protected static bool IsError<T>(SheerID.API.ServiceResponse<T> serviceResponse)
        {
            if (!serviceResponse.Success)
            {
                var error = serviceResponse.GetError();
                output.OutputObject(error);
                Console.ReadKey();
                return true;
            }
            return false;
        }
        protected static bool IsZeroCount(System.Collections.ICollection collection)
        {
            if (collection.Count == 0)
            {
                Console.WriteLine("0 matchs found.");
                Console.ReadKey();
                return true;
            }
            return false;
        }
    }
}
