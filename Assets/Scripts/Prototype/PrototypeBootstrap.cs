using UnityEngine;
using UnityEngine.EventSystems;

public class PrototypeBootstrap : MonoBehaviour
{
    private GameManager gameManager;
    private CardDeckManager cardDeckManager;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void AutoStartPrototype()
    {
        if (FindObjectOfType<PrototypeBootstrap>() != null)
        {
            return;
        }

        GameObject bootstrapObject = new GameObject("PrototypeBootstrap");
        bootstrapObject.AddComponent<PrototypeBootstrap>();
    }

    private void Awake()
    {
        EnsureEventSystem();

        cardDeckManager = FindObjectOfType<CardDeckManager>();
        if (cardDeckManager == null)
        {
            cardDeckManager = gameObject.AddComponent<CardDeckManager>();
        }

        gameManager = new GameManager(cardDeckManager);

        GameObject hudObject = new GameObject("PrototypeHUD");
        PrototypeHUD hud = hudObject.AddComponent<PrototypeHUD>();
        hud.Initialize(gameManager);

        gameManager.StartNewGame();
    }

    private static void EnsureEventSystem()
    {
        if (FindObjectOfType<EventSystem>() != null)
        {
            return;
        }

        GameObject eventSystemObject = new GameObject("EventSystem");
        eventSystemObject.AddComponent<EventSystem>();
        eventSystemObject.AddComponent<StandaloneInputModule>();
    }
}
