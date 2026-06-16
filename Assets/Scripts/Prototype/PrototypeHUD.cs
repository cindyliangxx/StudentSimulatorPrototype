using UnityEngine;
using UnityEngine.UI;

public class PrototypeHUD : MonoBehaviour
{
    private const float ChoiceButtonHeight = 72f;

    private GameManager gameManager;
    private Text titleText;
    private Text descriptionText;
    private Text statsText;
    private Text progressText;
    private Text specialText;
    private Text storyFlagsText;
    private Text endingText;
    private Button leftButton;
    private Button rightButton;
    private Image leftButtonImage;
    private Image rightButtonImage;
    private Text leftButtonText;
    private Text rightButtonText;
    private Font uiFont;
    private CardDragController cardDragController;
    private readonly Color normalButtonColor = new Color(0.88f, 0.9f, 0.93f);
    private readonly Color highlightedButtonColor = new Color(0.78f, 0.88f, 1f);

    public void Initialize(GameManager manager)
    {
        gameManager = manager;
        uiFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

        BuildCanvas();
        gameManager.StateChanged += Refresh;
        gameManager.PlayerStats.StatsChanged += Refresh;
        Refresh();
    }

    private void OnDestroy()
    {
        if (gameManager == null)
        {
            return;
        }

        gameManager.StateChanged -= Refresh;
        gameManager.PlayerStats.StatsChanged -= Refresh;
    }

    private void BuildCanvas()
    {
        Canvas canvas = gameObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        gameObject.AddComponent<CanvasScaler>().uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        gameObject.AddComponent<GraphicRaycaster>();

        GameObject root = CreatePanel("Root", transform);
        RectTransform rootRect = root.GetComponent<RectTransform>();
        rootRect.anchorMin = Vector2.zero;
        rootRect.anchorMax = Vector2.one;
        rootRect.offsetMin = new Vector2(32f, 24f);
        rootRect.offsetMax = new Vector2(-32f, -24f);

        VerticalLayoutGroup rootLayout = root.AddComponent<VerticalLayoutGroup>();
        rootLayout.spacing = 12f;
        rootLayout.childAlignment = TextAnchor.UpperCenter;
        rootLayout.childControlWidth = true;
        rootLayout.childControlHeight = true;
        rootLayout.childForceExpandWidth = true;
        rootLayout.childForceExpandHeight = false;

        GameObject cardPanel = CreatePanel("DraggableCard", root.transform);
        Image cardImage = cardPanel.AddComponent<Image>();
        cardImage.color = new Color(0.96f, 0.96f, 0.94f);
        VerticalLayoutGroup cardLayout = cardPanel.AddComponent<VerticalLayoutGroup>();
        cardLayout.padding = new RectOffset(20, 20, 18, 18);
        cardLayout.spacing = 10f;
        cardLayout.childControlWidth = true;
        cardLayout.childControlHeight = true;
        cardLayout.childForceExpandWidth = true;
        cardLayout.childForceExpandHeight = false;
        cardPanel.AddComponent<LayoutElement>().preferredHeight = 210f;

        cardDragController = cardPanel.AddComponent<CardDragController>();
        cardDragController.Initialize(gameManager, this, canvas);

        titleText = CreateText("Title", cardPanel.transform, 32, FontStyle.Bold, TextAnchor.MiddleCenter);
        descriptionText = CreateText("Description", cardPanel.transform, 20, FontStyle.Normal, TextAnchor.UpperCenter);
        descriptionText.GetComponent<LayoutElement>().preferredHeight = 110f;

        statsText = CreateText("Stats", root.transform, 18, FontStyle.Normal, TextAnchor.MiddleLeft);
        progressText = CreateText("Progress", root.transform, 18, FontStyle.Normal, TextAnchor.MiddleLeft);
        specialText = CreateText("Special", root.transform, 18, FontStyle.Normal, TextAnchor.MiddleLeft);
        storyFlagsText = CreateText("StoryFlags", root.transform, 18, FontStyle.Normal, TextAnchor.MiddleLeft);
        endingText = CreateText("Ending", root.transform, 20, FontStyle.Bold, TextAnchor.MiddleCenter);
        endingText.color = new Color(0.85f, 0.1f, 0.1f);

        GameObject buttonRow = CreatePanel("ChoiceButtons", root.transform);
        HorizontalLayoutGroup buttonLayout = buttonRow.AddComponent<HorizontalLayoutGroup>();
        buttonLayout.spacing = 16f;
        buttonLayout.childControlWidth = true;
        buttonLayout.childControlHeight = true;
        buttonLayout.childForceExpandWidth = true;
        buttonLayout.childForceExpandHeight = true;

        LayoutElement buttonRowLayout = buttonRow.AddComponent<LayoutElement>();
        buttonRowLayout.minHeight = ChoiceButtonHeight;
        buttonRowLayout.preferredHeight = ChoiceButtonHeight;
        buttonRowLayout.flexibleHeight = 0f;

        leftButton = CreateButton("LeftButton", buttonRow.transform, out leftButtonText, out leftButtonImage);
        rightButton = CreateButton("RightButton", buttonRow.transform, out rightButtonText, out rightButtonImage);

        leftButton.onClick.AddListener(OnLeftClicked);
        rightButton.onClick.AddListener(OnRightClicked);
    }

    private GameObject CreatePanel(string objectName, Transform parent)
    {
        GameObject panel = new GameObject(objectName, typeof(RectTransform));
        panel.transform.SetParent(parent, false);
        return panel;
    }

    private Text CreateText(string objectName, Transform parent, int fontSize, FontStyle fontStyle, TextAnchor alignment)
    {
        GameObject textObject = new GameObject(objectName, typeof(RectTransform));
        textObject.transform.SetParent(parent, false);

        Text text = textObject.AddComponent<Text>();
        text.font = uiFont;
        text.fontSize = fontSize;
        text.fontStyle = fontStyle;
        text.alignment = alignment;
        text.color = Color.black;
        text.horizontalOverflow = HorizontalWrapMode.Wrap;
        text.verticalOverflow = VerticalWrapMode.Truncate;

        LayoutElement layoutElement = textObject.AddComponent<LayoutElement>();
        layoutElement.minHeight = Mathf.Max(32f, fontSize * 1.6f);

        return text;
    }

    private Button CreateButton(string objectName, Transform parent, out Text label, out Image image)
    {
        GameObject buttonObject = new GameObject(objectName, typeof(RectTransform));
        buttonObject.transform.SetParent(parent, false);

        RectTransform buttonRect = buttonObject.GetComponent<RectTransform>();
        buttonRect.anchorMin = new Vector2(0f, 0.5f);
        buttonRect.anchorMax = new Vector2(1f, 0.5f);
        buttonRect.pivot = new Vector2(0.5f, 0.5f);
        buttonRect.sizeDelta = new Vector2(0f, ChoiceButtonHeight);

        LayoutElement buttonLayout = buttonObject.AddComponent<LayoutElement>();
        buttonLayout.minHeight = ChoiceButtonHeight;
        buttonLayout.preferredHeight = ChoiceButtonHeight;
        buttonLayout.flexibleHeight = 0f;

        image = buttonObject.AddComponent<Image>();
        image.color = normalButtonColor;

        Button button = buttonObject.AddComponent<Button>();
        ColorBlock colors = button.colors;
        colors.highlightedColor = new Color(0.78f, 0.82f, 0.88f);
        colors.pressedColor = new Color(0.68f, 0.73f, 0.8f);
        colors.disabledColor = new Color(0.65f, 0.65f, 0.65f);
        button.colors = colors;

        label = CreateText("Text", buttonObject.transform, 18, FontStyle.Bold, TextAnchor.MiddleCenter);
        RectTransform labelRect = label.GetComponent<RectTransform>();
        labelRect.anchorMin = Vector2.zero;
        labelRect.anchorMax = Vector2.one;
        labelRect.offsetMin = new Vector2(12f, 8f);
        labelRect.offsetMax = new Vector2(-12f, -8f);

        return button;
    }

    public void SetDragHint(float horizontalOffset)
    {
        if (gameManager == null || gameManager.IsGameOver)
        {
            ClearDragHint();
            return;
        }

        ClearDragHint();

        if (horizontalOffset < -20f)
        {
            leftButtonImage.color = highlightedButtonColor;
            leftButtonText.color = Color.black;
            leftButtonText.fontStyle = FontStyle.Bold;
            rightButtonText.color = new Color(0.35f, 0.35f, 0.35f);
        }
        else if (horizontalOffset > 20f)
        {
            rightButtonImage.color = highlightedButtonColor;
            rightButtonText.color = Color.black;
            rightButtonText.fontStyle = FontStyle.Bold;
            leftButtonText.color = new Color(0.35f, 0.35f, 0.35f);
        }
    }

    public void ClearDragHint()
    {
        if (leftButtonImage == null || rightButtonImage == null)
        {
            return;
        }

        leftButtonImage.color = normalButtonColor;
        rightButtonImage.color = normalButtonColor;
        leftButtonText.color = Color.black;
        rightButtonText.color = Color.black;
        leftButtonText.fontStyle = FontStyle.Bold;
        rightButtonText.fontStyle = FontStyle.Bold;
    }

    private void Refresh()
    {
        if (gameManager == null)
        {
            return;
        }

        EventCardData card = gameManager.CurrentCard;
        bool hasCard = card != null;

        titleText.text = gameManager.IsGameOver ? gameManager.EndingTitle : hasCard ? card.Title : "No Card";
        descriptionText.text = gameManager.IsGameOver ? gameManager.EndingDescription : hasCard ? card.Description : "No event card is available.";
        leftButtonText.text = hasCard ? card.LeftChoiceText : "Left";
        rightButtonText.text = hasCard ? card.RightChoiceText : "Right";

        statsText.text =
            $"Body: {gameManager.PlayerStats.GetValue(StatType.Body)}   " +
            $"Mental: {gameManager.PlayerStats.GetValue(StatType.Mental)}   " +
            $"Academic: {gameManager.PlayerStats.GetValue(StatType.Academic)}   " +
            $"Social: {gameManager.PlayerStats.GetValue(StatType.Social)}   " +
            $"Money: {gameManager.PlayerStats.GetValue(StatType.Money)}";

        progressText.text =
            $"Completed normal events: {gameManager.TotalNormalCardsCompleted}   " +
            $"Resolved events: {gameManager.TotalResolvedEvents}/{gameManager.MaxEventsBeforeEnding}";
        specialText.text = $"Special event active: {(gameManager.IsCurrentCardSpecial ? "Yes" : "No")}";
        storyFlagsText.text = $"Story flags: {GetStoryFlagsText()}";
        endingText.text = gameManager.IsGameOver ? $"Ending type: {gameManager.CurrentEndingType}" : string.Empty;

        leftButton.interactable = hasCard && !gameManager.IsGameOver;
        rightButton.interactable = hasCard && !gameManager.IsGameOver;
        cardDragController.ResetCardPosition();
        ClearDragHint();
    }

    private void OnLeftClicked()
    {
        gameManager.ChooseLeft();
    }

    private void OnRightClicked()
    {
        gameManager.ChooseRight();
    }

    private string GetStoryFlagsText()
    {
        System.Collections.Generic.List<string> flags = gameManager.StoryFlags.GetAllFlags();
        if (flags.Count == 0)
        {
            return "None";
        }

        return string.Join(", ", flags);
    }
}
