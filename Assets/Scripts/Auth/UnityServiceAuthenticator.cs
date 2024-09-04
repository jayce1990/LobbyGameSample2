using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Unity.Services.Authentication;
using Unity.Services.Core;

public static class UnityServiceAuthenticator
{
    const int k_InitTimeout = 10000;
    static bool s_IsSigningIn;

    public static async Task<bool> TryInitServicesAsync(string profileName = null)
    {
        if (UnityServices.State == ServicesInitializationState.Initialized)
            return true;

        if (UnityServices.State == ServicesInitializationState.Initializing)
        {
            var task = WaitForInitialized();
            if (await Task.WhenAny(task, Task.Delay(k_InitTimeout)) != task)
                return false;//We timed out

            return UnityServices.State == ServicesInitializationState.Initialized;
        }

        if (profileName != null)
        {
            Regex regex = new Regex("[^a-zA-Z0-9 - _]");
            profileName = regex.Replace(profileName, "");
            var authProfile = new InitializationOptions().SetProfile(profileName);

            //使用多个Unity服务(Lobby、Relay等),只初始化一次即可。
            await UnityServices.InitializeAsync(authProfile);
        }
        else
            await UnityServices.InitializeAsync();

        return UnityServices.State == ServicesInitializationState.Initialized;

        async Task WaitForInitialized()
        {
            while (UnityServices.State != ServicesInitializationState.Initialized)
                await Task.Delay(100);
        }
    }

    public static async Task<bool> TrySignInAsync(string profileName = null)
    {
        if (!await TryInitServicesAsync(profileName))
            return false;
        if (s_IsSigningIn)//已有匿名验证在请求中,再次调用需等待返回结果,无需再次请求
        {
            var task = WaitForSignedIn();
            if (await Task.WhenAny(task, Task.Delay(k_InitTimeout)) != task)
                return false;//We timed out
            return AuthenticationService.Instance.IsSignedIn;
        }

        s_IsSigningIn = true;
        await AuthenticationService.Instance.SignInAnonymouslyAsync();
        s_IsSigningIn = false;

        return AuthenticationService.Instance.IsSignedIn;

        async Task WaitForSignedIn()
        {
            while (!AuthenticationService.Instance.IsSignedIn)
            {
                await Task.Delay(100);
            }
        }
    }

    //AuthenticationService.Instance.SignOut();
}
