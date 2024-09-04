using System.Collections;
using System.Collections.Generic;
using Unity.Services.Authentication;
using UnityEngine;

public class Test : MonoBehaviour
{
    // Start is called before the first frame update
    async void Start()
    {
        await UnityServiceAuthenticator.TrySignInAsync("Jacy1990");
        Debug.Log(AuthenticationService.Instance.PlayerId);
        LogHandlerSettings.Instance.SpawnErrorPopup(AuthenticationService.Instance.PlayerId);

        //eIru6liVCtZur68O4BPvxrZsPaad
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
