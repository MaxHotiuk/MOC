using System.Text;
using Core.Interfaces;

namespace Infrastructure.Services;

public class KnapsackCipherService : IKnapsackCipherService
{
    private string ChooseAlphabet(string language)
    {
        string eng_alphabet = " ,.abcdefghijklmnopqrstuvwxyz";
        string ukr_alphabet = " ,.абвгґдеєжзиіїйклмнопрстуфхцчшщьюя";
        return language == "eng" ? eng_alphabet : ukr_alphabet;
    }
    
    public List<int> GenerateSuperIncreasingSequence(int size)
    {
        List<int> sequence = new List<int>();
        Random rand = new Random();
        
        int value = rand.Next(1, 10);
        sequence.Add(value);
        
        for (int i = 1; i < size; i++)
        {
            int sum = sequence.Sum();
            value = sum + rand.Next(1, 10);
            sequence.Add(value);
        }
        
        return sequence;
    }
    
    public int FindCoprime(int n)
    {
        Random rand = new Random();
        int result;
        
        do
        {
            result = rand.Next(2, n);
        } while (GCD(result, n) != 1);
        
        return result;
    }
    
    private int GCD(int a, int b)
    {
        while (b != 0)
        {
            int temp = b;
            b = a % b;
            a = temp;
        }
        return a;
    }
    
    public int ModInverse(int a, int m)
    {
        int m0 = m;
        int y = 0, x = 1;
        
        if (m == 1)
            return 0;
            
        while (a > 1)
        {
            int q = a / m;
            int t = m;
            
            m = a % m;
            a = t;
            t = y;
            
            y = x - q * y;
            x = t;
        }
        
        if (x < 0)
            x += m0;
            
        return x;
    }
    
    public List<int> GeneratePublicKey(List<int> privateKey, int m, int n)
    {
        List<int> publicKey = new List<int>();
        
        foreach (int value in privateKey)
        {
            int publicValue = (value * m) % n;
            publicKey.Add(publicValue);
        }
        
        return publicKey;
    }
    
    public List<int> Encrypt(string message, List<int> publicKey, string language)
    {
        string alphabet = ChooseAlphabet(language);
        List<int> encrypted = new List<int>();
        
        foreach (char c in message.ToLower())
        {
            int index = alphabet.IndexOf(c);
            if (index == -1) continue;
            
            string binary = Convert.ToString(index, 2).PadLeft(8, '0');
            
            int sum = 0;
            for (int i = 0; i < binary.Length && i < publicKey.Count; i++)
            {
                if (binary[i] == '1')
                {
                    sum += publicKey[i];
                }
            }
            
            encrypted.Add(sum);
        }
        
        return encrypted;
    }
    
    public string Decrypt(List<int> encrypted, List<int> privateKey, int m, int n, string language)
    {
        string alphabet = ChooseAlphabet(language);
        StringBuilder decrypted = new StringBuilder();
        
        int m_inverse = ModInverse(m, n);
        
        foreach (int value in encrypted)
        {
            int c_prime = (value * m_inverse) % n;
            
            string binary = "";
            for (int i = privateKey.Count - 1; i >= 0; i--)
            {
                if (c_prime >= privateKey[i])
                {
                    binary = "1" + binary;
                    c_prime -= privateKey[i];
                }
                else
                {
                    binary = "0" + binary;
                }
            }
            
            int index = Convert.ToInt32(binary, 2);
            if (index >= 0 && index < alphabet.Length)
            {
                decrypted.Append(alphabet[index]);
            }
        }
        
        return decrypted.ToString();
    }
    
    public (List<int> privateKey, List<int> publicKey, int m, int n) GenerateKeyPair(int size = 8)
    {
        List<int> privateKey = GenerateSuperIncreasingSequence(size);
        
        int n = privateKey.Sum() + new Random().Next(10, 100);
        
        int m = FindCoprime(n);
        
        List<int> publicKey = GeneratePublicKey(privateKey, m, n);
        
        return (privateKey, publicKey, m, n);
    }
}