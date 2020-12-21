using CredentialManagement;

namespace DbCourseWork
{
    static class PasswordManager
    {
        /*public class CredentialData
        {
            public string Password { get; set; }
            public string User { get; set; }
        }

        public static void SaveCredentialData(string passwordKey, string username, string password)
        {
            using (var cred = new Credential())
            {
                cred.Target = passwordKey;
                cred.Password = password;
                cred.Username = username;
                cred.Type = CredentialType.Generic;
                cred.PersistanceType = PersistanceType.LocalComputer;
                cred.Save();
            }
        }

        public static CredentialData GetCredentialData(string passwordKey)
        {
            using (var cred = new Credential())
            {
                cred.Target = passwordKey;
                cred.Load();
                return new CredentialData {Password = cred.Password, User = cred.Username};
            }
        }

        public static bool RemoveCredentials(string target)
        {
            return new Credential { Target = target }.Delete();
        }*/
    }
}
