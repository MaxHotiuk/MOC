using System.Numerics;

namespace Core.Interfaces
{
    public interface IKnapsackCipherService
    {
        List<int> GenerateSuperIncreasingSequence(int size);
        int FindCoprime(int n);
        int ModInverse(int a, int m);
        List<int> GeneratePublicKey(List<int> privateKey, int m, int n);
        List<int> Encrypt(string message, List<int> publicKey, string language);
        string Decrypt(List<int> encrypted, List<int> privateKey, int m, int n, string language);
        (List<int> privateKey, List<int> publicKey, int m, int n) GenerateKeyPair(int size = 8);
    }
}
