using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using System.ComponentModel.DataAnnotations;
using System.Text;
using СourseworkBackend.Models;

namespace СourseworkBackend
{
    public enum SessionStatus
    {
        Valid,
        Invalid,
        Expired
    }

    public class GlobalScope
    {
        public static ProjectDatabase database = new ProjectDatabase();

        public static Session? GetSession(string base64EncocodedSessionToken)
        {
            byte[] token;
            try
            {
                token = Convert.FromBase64String(base64EncocodedSessionToken);
            }
            catch
            {
                return null;
            }

            return database.Sessions.Where(sessions => sessions.Token == token).FirstOrDefault();
        }

        public static SessionStatus GetSessionStatus(Session? session)
        {
            if (session == null)
                return SessionStatus.Invalid;

            if (DateTime.Now.Subtract(session.LastTokenUseTime).TotalHours > 24)
            {
                database.Sessions.Remove(session);
                database.SaveChangesAsync().Wait();

                return SessionStatus.Expired;
            }    

            return SessionStatus.Valid;
        }

        public static byte[] GetHashSha3(string input)
        {
            var hashAlgorithm = new Org.BouncyCastle.Crypto.Digests.Sha3Digest(512);

            // Choose correct encoding based on your usecase
            byte[] inputArray = Encoding.Unicode.GetBytes(input);

            hashAlgorithm.BlockUpdate(inputArray, 0, inputArray.Length);

            byte[] result = new byte[64]; // 512 / 8 = 64
            hashAlgorithm.DoFinal(result, 0);

            return result;
        }
        public static byte[] GetHashSha3(byte[] input)
        {
            var hashAlgorithm = new Org.BouncyCastle.Crypto.Digests.Sha3Digest(512);

            hashAlgorithm.BlockUpdate(input, 0, input.Length);

            byte[] result = new byte[64]; // 512 / 8 = 64
            hashAlgorithm.DoFinal(result, 0);

            return result;
        }


    }
}
