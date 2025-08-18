namespace TheImitationGame.Api.Interfaces
{
    public interface IImitationGenerator
    {
        Task<List<string>> GenerateImitations(string prompt, string image_b64, int amount);
    }
}
