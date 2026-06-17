using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
#if UNITY_EDITOR
using UnityEditor;
#endif

public class PrototypeHUD : MonoBehaviour
{
    private const string MainGameUIResourceLibraryPath = "Assets/ResourceLibrary/MainGameUI";
    private const float DragPreviewDeadZone = 90f;
    private const float StatIconFillAnimationDuration = 1.35f;
    private const float StatIconColorAnimationDuration = 0.75f;

    private static readonly StatType[] StatDisplayOrder =
    {
        StatType.Health,
        StatType.Money,
        StatType.Social,
        StatType.Academic
    };

    private readonly Color backgroundColor = new Color32(0xF4, 0xF0, 0xE8, 0xFF);
    private readonly Color artworkFillColor = new Color32(0xE8, 0xE1, 0xD6, 0xFF);
    private readonly Color artworkStrokeColor = new Color32(0xC9, 0xC0, 0xB2, 0xFF);
    private readonly Color mainTextColor = new Color32(0x30, 0x32, 0x30, 0xFF);
    private readonly Color secondaryTextColor = new Color32(0x6E, 0x71, 0x6D, 0xFF);
    private readonly Color mutedTextColor = new Color32(0x8D, 0x8B, 0x82, 0xFF);
    private readonly Color accentColor = new Color32(0x6D, 0x81, 0x78, 0xFF);
    private readonly Color statIconFillColor = new Color32(0xD9, 0xB9, 0x6A, 0xFF);
    private readonly Color statIconOutlineColor = new Color32(0x2A, 0x18, 0x08, 0xFF);
    private readonly Color entryButtonFillColor = new Color32(0xF8, 0xF5, 0xEE, 0xCC);
    private readonly Color entryButtonStrokeColor = new Color32(0xC9, 0xC0, 0xB2, 0xFF);
    private readonly Color inactiveChoiceColor = new Color32(0x64, 0x66, 0x5F, 0xFF);
    private readonly Color positiveColor = new Color32(0x6F, 0x92, 0x7E, 0xFF);
    private readonly Color negativeColor = new Color32(0xB0, 0x6A, 0x5F, 0xFF);
    private readonly Color transparentColor = new Color(1f, 1f, 1f, 0f);

    private GameManager gameManager;
    private Font uiFont;
    private Text titleText;
    private Image cardArtworkImage;
    private Text cardPlaceholderText;
    private RoundedRectGraphic artworkFrame;
    private Text descriptionText;
    private Text currentStateText;
    private Text inventoryText;
    private Text timelineText;
    private Button leftButton;
    private Button rightButton;
    private Image leftButtonImage;
    private Image rightButtonImage;
    private Text leftButtonText;
    private Text rightButtonText;
    private CardDragController cardDragController;
    private readonly Dictionary<StatType, StatWidget> statWidgets = new Dictionary<StatType, StatWidget>();

    public void Initialize(GameManager manager)
    {
        gameManager = manager;
        uiFont = ResolveUIFont();

        BuildCanvas();
        gameManager.StateChanged += Refresh;
        gameManager.PlayerStats.StatsChanged += RefreshStatsOnly;
        Refresh();
    }

    private void OnDestroy()
    {
        if (gameManager == null)
        {
            return;
        }

        gameManager.StateChanged -= Refresh;
        gameManager.PlayerStats.StatsChanged -= RefreshStatsOnly;
    }

    private void Update()
    {
        foreach (StatWidget statWidget in statWidgets.Values)
        {
            statWidget.UpdateAnimation(Time.deltaTime);
        }

        if (gameManager == null || gameManager.IsGameOver || gameManager.CurrentCard == null)
        {
            return;
        }

        if (Input.GetKeyDown(KeyCode.LeftArrow) || Input.GetKeyDown(KeyCode.A))
        {
            gameManager.ChooseLeft();
        }
        else if (Input.GetKeyDown(KeyCode.RightArrow) || Input.GetKeyDown(KeyCode.D))
        {
            gameManager.ChooseRight();
        }
    }

    private void BuildCanvas()
    {
        Canvas canvas = gameObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;

        CanvasScaler scaler = gameObject.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;

        gameObject.AddComponent<GraphicRaycaster>();

        GameObject root = CreateRectObject("Root", transform);
        Image rootImage = root.AddComponent<Image>();
        Sprite backgroundSprite = LoadEditorBackgroundSprite();
        if (backgroundSprite != null)
        {
            rootImage.sprite = backgroundSprite;
            rootImage.color = Color.white;
            rootImage.type = Image.Type.Simple;
            rootImage.preserveAspect = false;
        }
        else
        {
            rootImage.color = backgroundColor;
        }

        RectTransform rootRect = root.GetComponent<RectTransform>();
        StretchToParent(rootRect, Vector2.zero, Vector2.zero);

        BuildStatsRow(root.transform);
        BuildCardStage(root.transform, canvas);
        BuildBottomInfo(root.transform);
    }

    private void BuildStatsRow(Transform root)
    {
        GameObject statsRow = CreateRectObject("StatsRow", root);
        RectTransform statsRect = statsRow.GetComponent<RectTransform>();
        statsRect.anchorMin = new Vector2(0f, 1f);
        statsRect.anchorMax = new Vector2(1f, 1f);
        statsRect.pivot = new Vector2(0.5f, 1f);
        statsRect.offsetMin = new Vector2(190f, -214f);
        statsRect.offsetMax = new Vector2(-250f, -24f);

        HorizontalLayoutGroup layout = statsRow.AddComponent<HorizontalLayoutGroup>();
        layout.spacing = 88f;
        layout.childAlignment = TextAnchor.MiddleCenter;
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = true;

        foreach (StatType statType in StatDisplayOrder)
        {
            statWidgets[statType] = CreateStatWidget(statType, statsRow.transform);
        }
    }

    private void BuildCardStage(Transform root, Canvas canvas)
    {
        GameObject stage = CreateRectObject("CardStage", root);
        StretchToParent(stage.GetComponent<RectTransform>(), Vector2.zero, Vector2.zero);

        leftButton = CreateChoiceButton(
            "LeftChoice",
            stage.transform,
            new Vector2(0.5f, 0.5f),
            new Vector2(-570f, 68f),
            TextAnchor.MiddleCenter,
            out leftButtonText,
            out leftButtonImage);
        rightButton = CreateChoiceButton(
            "RightChoice",
            stage.transform,
            new Vector2(0.5f, 0.5f),
            new Vector2(570f, 68f),
            TextAnchor.MiddleCenter,
            out rightButtonText,
            out rightButtonImage);

        leftButton.onClick.AddListener(OnLeftClicked);
        rightButton.onClick.AddListener(OnRightClicked);

        GameObject cardPanel = CreateRectObject("DraggableCard", stage.transform);
        RectTransform cardRect = cardPanel.GetComponent<RectTransform>();
        cardRect.anchorMin = new Vector2(0.5f, 0.5f);
        cardRect.anchorMax = new Vector2(0.5f, 0.5f);
        cardRect.pivot = new Vector2(0.5f, 0.5f);
        cardRect.anchoredPosition = new Vector2(0f, 124f);
        cardRect.sizeDelta = new Vector2(560f, 420f);

        // Transparent hit area keeps the card draggable without adding a visible panel.
        Image dragHitArea = cardPanel.AddComponent<Image>();
        dragHitArea.color = transparentColor;

        GameObject frameObject = CreateRectObject("ArtworkFrame", cardPanel.transform);
        StretchToParent(frameObject.GetComponent<RectTransform>(), Vector2.zero, Vector2.zero);
        artworkFrame = frameObject.AddComponent<RoundedRectGraphic>();
        artworkFrame.raycastTarget = false;
        artworkFrame.SetStyle(artworkFillColor, artworkStrokeColor, 20f, 2f);

        GameObject artworkObject = CreateRectObject("CardArtworkImage", cardPanel.transform);
        RectTransform artworkRect = artworkObject.GetComponent<RectTransform>();
        StretchToParent(artworkRect, new Vector2(10f, 10f), new Vector2(-10f, -10f));
        cardArtworkImage = artworkObject.AddComponent<Image>();
        cardArtworkImage.color = Color.clear;
        cardArtworkImage.preserveAspect = true;
        cardArtworkImage.raycastTarget = false;

        cardPlaceholderText = CreateText(
            "CardPlaceholder",
            cardPanel.transform,
            24,
            FontStyle.Normal,
            TextAnchor.MiddleCenter,
            mutedTextColor);
        StretchToParent(cardPlaceholderText.GetComponent<RectTransform>(), Vector2.zero, Vector2.zero);
        cardPlaceholderText.text = "卡片图片";

        cardDragController = cardPanel.AddComponent<CardDragController>();
        cardDragController.Initialize(gameManager, this, canvas);
        cardDragController.DragThreshold = 220f;

        GameObject narrativeArea = CreateRectObject("NarrativeArea", stage.transform);
        RectTransform narrativeRect = narrativeArea.GetComponent<RectTransform>();
        narrativeRect.anchorMin = new Vector2(0.5f, 0.5f);
        narrativeRect.anchorMax = new Vector2(0.5f, 0.5f);
        narrativeRect.pivot = new Vector2(0.5f, 0.5f);
        narrativeRect.anchoredPosition = new Vector2(0f, -286f);
        narrativeRect.sizeDelta = new Vector2(820f, 190f);

        titleText = CreateText("Title", narrativeArea.transform, 24, FontStyle.Bold, TextAnchor.MiddleCenter, mainTextColor);
        RectTransform titleRect = titleText.GetComponent<RectTransform>();
        titleRect.anchorMin = new Vector2(0f, 1f);
        titleRect.anchorMax = new Vector2(1f, 1f);
        titleRect.pivot = new Vector2(0.5f, 1f);
        titleRect.offsetMin = new Vector2(0f, -44f);
        titleRect.offsetMax = new Vector2(0f, 0f);

        descriptionText = CreateText("Description", narrativeArea.transform, 30, FontStyle.Normal, TextAnchor.UpperCenter, mainTextColor);
        RectTransform descriptionRect = descriptionText.GetComponent<RectTransform>();
        descriptionRect.anchorMin = new Vector2(0f, 0f);
        descriptionRect.anchorMax = new Vector2(1f, 1f);
        descriptionRect.offsetMin = new Vector2(0f, 0f);
        descriptionRect.offsetMax = new Vector2(0f, -54f);
        descriptionText.lineSpacing = 1.15f;
        descriptionText.resizeTextForBestFit = true;
        descriptionText.resizeTextMinSize = 24;
        descriptionText.resizeTextMaxSize = 30;
    }

    private void BuildBottomInfo(Transform root)
    {
        GameObject leftInfo = CreateRectObject("StatusAndItems", root);
        RectTransform leftRect = leftInfo.GetComponent<RectTransform>();
        leftRect.anchorMin = new Vector2(0f, 0f);
        leftRect.anchorMax = new Vector2(0f, 0f);
        leftRect.pivot = new Vector2(0f, 0f);
        leftRect.anchoredPosition = new Vector2(88f, 58f);
        leftRect.sizeDelta = new Vector2(620f, 86f);

        currentStateText = CreateInfoLine(
            "CurrentState",
            leftInfo.transform,
            SimpleIconKind.Status,
            new Vector2(0f, 46f),
            "当前状态：普通");
        inventoryText = CreateInfoLine(
            "Inventory",
            leftInfo.transform,
            SimpleIconKind.Item,
            new Vector2(0f, 8f),
            "持有道具：学生证");

        GameObject rightInfo = CreateRectObject("CornerEntries", root);
        RectTransform rightRect = rightInfo.GetComponent<RectTransform>();
        rightRect.anchorMin = new Vector2(1f, 0f);
        rightRect.anchorMax = new Vector2(1f, 0f);
        rightRect.pivot = new Vector2(1f, 0f);
        rightRect.anchoredPosition = new Vector2(-88f, 58f);
        rightRect.sizeDelta = new Vector2(410f, 52f);

        HorizontalLayoutGroup layout = rightInfo.AddComponent<HorizontalLayoutGroup>();
        layout.spacing = 28f;
        layout.childAlignment = TextAnchor.MiddleRight;
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = false;
        layout.childForceExpandHeight = true;

        Button timelineButton = CreateIconTextButton("TimelineEntry", rightInfo.transform, SimpleIconKind.Timeline, "时间轴 0/0", out timelineText);
        Button achievementButton = CreateIconTextButton("AchievementEntry", rightInfo.transform, SimpleIconKind.Achievement, "成就", out _);
        timelineButton.interactable = false;
        achievementButton.interactable = false;
    }

    private Button CreateChoiceButton(
        string objectName,
        Transform parent,
        Vector2 anchor,
        Vector2 anchoredPosition,
        TextAnchor textAnchor,
        out Text label,
        out Image image)
    {
        GameObject buttonObject = CreateRectObject(objectName, parent);
        RectTransform buttonRect = buttonObject.GetComponent<RectTransform>();
        buttonRect.anchorMin = anchor;
        buttonRect.anchorMax = anchor;
        buttonRect.pivot = new Vector2(0.5f, 0.5f);
        buttonRect.anchoredPosition = anchoredPosition;
        buttonRect.sizeDelta = new Vector2(430f, 200f);

        image = buttonObject.AddComponent<Image>();
        image.color = transparentColor;

        Button button = buttonObject.AddComponent<Button>();
        button.targetGraphic = image;
        button.transition = Selectable.Transition.None;

        label = CreateText("Text", buttonObject.transform, 34, FontStyle.Bold, textAnchor, inactiveChoiceColor);
        RectTransform labelRect = label.GetComponent<RectTransform>();
        StretchToParent(labelRect, new Vector2(18f, 18f), new Vector2(-18f, -18f));
        label.resizeTextForBestFit = true;
        label.resizeTextMinSize = 24;
        label.resizeTextMaxSize = 34;

        return button;
    }

    private StatWidget CreateStatWidget(StatType statType, Transform parent)
    {
        GameObject panel = CreateRectObject($"{statType}Stat", parent);
        LayoutElement layoutElement = panel.AddComponent<LayoutElement>();
        layoutElement.minHeight = 190f;
        layoutElement.preferredHeight = 190f;
        layoutElement.minWidth = 250f;

        GameObject dotObject = CreateRectObject("ChangeDot", panel.transform);
        RectTransform dotRect = dotObject.GetComponent<RectTransform>();
        dotRect.anchorMin = new Vector2(0.5f, 1f);
        dotRect.anchorMax = new Vector2(0.5f, 1f);
        dotRect.pivot = new Vector2(0.5f, 1f);
        dotRect.anchoredPosition = new Vector2(0f, -4f);
        dotRect.sizeDelta = new Vector2(40f, 40f);
        StatChangeDotGraphic changeDot = dotObject.AddComponent<StatChangeDotGraphic>();
        changeDot.raycastTarget = false;
        changeDot.Hide();

        GameObject iconObject = CreateRectObject("FilledIcon", panel.transform);
        RectTransform iconRect = iconObject.GetComponent<RectTransform>();
        iconRect.anchorMin = new Vector2(0.5f, 0.5f);
        iconRect.anchorMax = new Vector2(0.5f, 0.5f);
        iconRect.pivot = new Vector2(0.5f, 0.5f);
        iconRect.anchoredPosition = new Vector2(0f, 8f);
        iconRect.sizeDelta = GetStatIconDisplaySize(statType);

        Image fillImage = CreateStatIconImage("Fill", iconObject.transform, LoadStatIconSprite(statType, isFillMask: true));
        fillImage.type = Image.Type.Filled;
        fillImage.fillMethod = Image.FillMethod.Vertical;
        fillImage.fillOrigin = (int)Image.OriginVertical.Bottom;
        fillImage.fillAmount = PlayerStats.InitialValue / 100f;
        fillImage.color = statIconFillColor;

        CreateStatIconImage("Outline", iconObject.transform, LoadStatIconSprite(statType, isFillMask: false));

        Text label = CreateText("Label", panel.transform, 22, FontStyle.Bold, TextAnchor.MiddleCenter, mainTextColor);
        RectTransform labelRect = label.GetComponent<RectTransform>();
        labelRect.anchorMin = new Vector2(0f, 0f);
        labelRect.anchorMax = new Vector2(1f, 0f);
        labelRect.pivot = new Vector2(0.5f, 0f);
        labelRect.offsetMin = new Vector2(0f, 0f);
        labelRect.offsetMax = new Vector2(0f, 34f);

        return new StatWidget(
            statType,
            label,
            fillImage,
            changeDot,
            statIconFillColor,
            statIconOutlineColor,
            positiveColor,
            negativeColor,
            StatIconFillAnimationDuration,
            StatIconColorAnimationDuration);
    }

    private Text CreateInfoLine(
        string objectName,
        Transform parent,
        SimpleIconKind iconKind,
        Vector2 anchoredPosition,
        string defaultText)
    {
        GameObject lineObject = CreateRectObject(objectName, parent);
        RectTransform lineRect = lineObject.GetComponent<RectTransform>();
        lineRect.anchorMin = new Vector2(0f, 0f);
        lineRect.anchorMax = new Vector2(1f, 0f);
        lineRect.pivot = new Vector2(0f, 0f);
        lineRect.anchoredPosition = anchoredPosition;
        lineRect.sizeDelta = new Vector2(0f, 30f);

        SimpleIconGraphic icon = CreateIcon("Icon", lineObject.transform, iconKind, new Vector2(22f, 22f), accentColor);
        RectTransform iconRect = icon.GetComponent<RectTransform>();
        iconRect.anchorMin = new Vector2(0f, 0.5f);
        iconRect.anchorMax = new Vector2(0f, 0.5f);
        iconRect.pivot = new Vector2(0f, 0.5f);
        iconRect.anchoredPosition = new Vector2(0f, 0f);

        Text text = CreateText("Text", lineObject.transform, 20, FontStyle.Normal, TextAnchor.MiddleLeft, secondaryTextColor);
        RectTransform textRect = text.GetComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = new Vector2(32f, 0f);
        textRect.offsetMax = Vector2.zero;
        text.text = defaultText;

        return text;
    }

    private Button CreateIconTextButton(
        string objectName,
        Transform parent,
        SimpleIconKind iconKind,
        string labelText,
        out Text label)
    {
        GameObject buttonObject = CreateRectObject(objectName, parent);
        RectTransform buttonRect = buttonObject.GetComponent<RectTransform>();
        buttonRect.sizeDelta = new Vector2(labelText.Length > 4 ? 188f : 88f, 46f);
        LayoutElement buttonLayout = buttonObject.AddComponent<LayoutElement>();
        buttonLayout.preferredWidth = labelText.Length > 4 ? 188f : 88f;
        buttonLayout.preferredHeight = 46f;

        RoundedRectGraphic background = buttonObject.AddComponent<RoundedRectGraphic>();
        background.SetStyle(entryButtonFillColor, entryButtonStrokeColor, 12f, 1.4f);

        Button button = buttonObject.AddComponent<Button>();
        button.targetGraphic = background;
        button.transition = Selectable.Transition.None;

        HorizontalLayoutGroup layout = buttonObject.AddComponent<HorizontalLayoutGroup>();
        layout.spacing = 8f;
        layout.padding = new RectOffset(0, 0, 0, 0);
        layout.childAlignment = TextAnchor.MiddleCenter;
        layout.childControlWidth = false;
        layout.childControlHeight = false;
        layout.childForceExpandWidth = false;
        layout.childForceExpandHeight = false;

        SimpleIconGraphic icon = CreateIcon("Icon", buttonObject.transform, iconKind, new Vector2(24f, 24f), accentColor);
        LayoutElement iconLayout = icon.gameObject.AddComponent<LayoutElement>();
        iconLayout.preferredWidth = 24f;
        iconLayout.preferredHeight = 24f;

        label = CreateText("Text", buttonObject.transform, 20, FontStyle.Normal, TextAnchor.MiddleLeft, secondaryTextColor);
        LayoutElement labelLayout = label.GetComponent<LayoutElement>();
        labelLayout.preferredWidth = labelText.Length > 4 ? 148f : 48f;
        labelLayout.preferredHeight = 34f;
        label.text = labelText;

        return button;
    }

    private SimpleIconGraphic CreateIcon(
        string objectName,
        Transform parent,
        SimpleIconKind iconKind,
        Vector2 size,
        Color iconColor)
    {
        GameObject iconObject = CreateRectObject(objectName, parent);
        RectTransform iconRect = iconObject.GetComponent<RectTransform>();
        iconRect.sizeDelta = size;

        SimpleIconGraphic icon = iconObject.AddComponent<SimpleIconGraphic>();
        icon.raycastTarget = false;
        icon.SetIcon(iconKind, iconColor);
        return icon;
    }

    private Image CreateStatIconImage(string objectName, Transform parent, Sprite sprite)
    {
        GameObject imageObject = CreateRectObject(objectName, parent);
        StretchToParent(imageObject.GetComponent<RectTransform>(), Vector2.zero, Vector2.zero);
        Image image = imageObject.AddComponent<Image>();
        image.sprite = sprite;
        image.preserveAspect = true;
        image.raycastTarget = false;
        image.color = sprite != null ? Color.white : Color.clear;
        return image;
    }

    private Text CreateText(string objectName, Transform parent, int fontSize, FontStyle fontStyle, TextAnchor alignment, Color color)
    {
        GameObject textObject = CreateRectObject(objectName, parent);

        Text text = textObject.AddComponent<Text>();
        text.font = uiFont;
        text.fontSize = fontSize;
        text.fontStyle = fontStyle;
        text.alignment = alignment;
        text.color = color;
        text.horizontalOverflow = HorizontalWrapMode.Wrap;
        text.verticalOverflow = VerticalWrapMode.Truncate;
        text.raycastTarget = false;
        text.supportRichText = false;

        LayoutElement layoutElement = textObject.AddComponent<LayoutElement>();
        layoutElement.minHeight = Mathf.Max(28f, fontSize * 1.45f);

        return text;
    }

    private GameObject CreateRectObject(string objectName, Transform parent)
    {
        GameObject rectObject = new GameObject(objectName, typeof(RectTransform));
        rectObject.transform.SetParent(parent, false);
        return rectObject;
    }

    private static void StretchToParent(RectTransform rectTransform, Vector2 offsetMin, Vector2 offsetMax)
    {
        rectTransform.anchorMin = Vector2.zero;
        rectTransform.anchorMax = Vector2.one;
        rectTransform.offsetMin = offsetMin;
        rectTransform.offsetMax = offsetMax;
    }

    public void SetDragHint(float horizontalOffset)
    {
        if (gameManager == null || gameManager.IsGameOver || gameManager.CurrentCard == null)
        {
            ClearDragHint();
            return;
        }

        if (horizontalOffset < -DragPreviewDeadZone)
        {
            SetChoiceVisuals(isLeftActive: true);
            ShowEffectPreview(gameManager.CurrentCard.LeftEffect);
        }
        else if (horizontalOffset > DragPreviewDeadZone)
        {
            SetChoiceVisuals(isLeftActive: false);
            ShowEffectPreview(gameManager.CurrentCard.RightEffect);
        }
        else
        {
            ClearDragHint();
        }
    }

    public void ClearDragHint()
    {
        if (leftButtonImage != null)
        {
            leftButtonImage.color = transparentColor;
        }

        if (rightButtonImage != null)
        {
            rightButtonImage.color = transparentColor;
        }

        if (leftButtonText != null)
        {
            leftButtonText.color = inactiveChoiceColor;
        }

        if (rightButtonText != null)
        {
            rightButtonText.color = inactiveChoiceColor;
        }

        foreach (StatWidget statWidget in statWidgets.Values)
        {
            statWidget.ClearPreview();
        }
    }

    private void SetChoiceVisuals(bool isLeftActive)
    {
        leftButtonImage.color = transparentColor;
        rightButtonImage.color = transparentColor;
        leftButtonText.color = isLeftActive ? accentColor : mutedTextColor;
        rightButtonText.color = isLeftActive ? mutedTextColor : accentColor;
    }

    private void ShowEffectPreview(ChoiceEffect effect)
    {
        if (gameManager == null)
        {
            return;
        }

        Dictionary<StatType, int> mergedChanges = effect != null
            ? effect.GetMergedStatChanges()
            : new Dictionary<StatType, int>();

        foreach (StatType statType in StatDisplayOrder)
        {
            mergedChanges.TryGetValue(statType, out int rawAmount);
            int effectiveAmount = gameManager.PlayerStats.GetEffectiveChange(statType, rawAmount);
            statWidgets[statType].ShowPreview(effectiveAmount);
        }
    }

    private void RefreshStatsOnly()
    {
        if (gameManager == null)
        {
            return;
        }

        foreach (StatType statType in StatDisplayOrder)
        {
            statWidgets[statType].Refresh(gameManager.PlayerStats.GetValue(statType));
        }
    }

    private void Refresh()
    {
        if (gameManager == null)
        {
            return;
        }

        RefreshStatsOnly();

        EventCardData card = gameManager.CurrentCard;
        bool hasCard = card != null;

        titleText.text = gameManager.IsGameOver ? gameManager.EndingTitle : hasCard ? card.Title : "暂无事件";
        descriptionText.text = gameManager.IsGameOver
            ? gameManager.EndingDescription
            : hasCard
                ? card.Description
                : "当前没有可显示的事件卡。";

        RefreshCardArtwork(card, gameManager.IsGameOver);

        leftButtonText.text = hasCard && !gameManager.IsGameOver ? card.LeftChoiceText : string.Empty;
        rightButtonText.text = hasCard && !gameManager.IsGameOver ? card.RightChoiceText : string.Empty;
        leftButton.interactable = hasCard && !gameManager.IsGameOver;
        rightButton.interactable = hasCard && !gameManager.IsGameOver;

        currentStateText.text = $"当前状态：{GetOverallStateText()}";
        inventoryText.text = "持有道具：学生证";
        timelineText.text = $"时间轴 {gameManager.TotalResolvedEvents}/{gameManager.MaxEventsBeforeEnding}";

        cardDragController.ResetCardPosition();
        ClearDragHint();
    }

    private void RefreshCardArtwork(EventCardData card, bool isGameOver)
    {
        if (cardArtworkImage == null)
        {
            return;
        }

        Sprite sprite = card != null ? card.CardImage : null;
        bool hasSprite = sprite != null;
        cardArtworkImage.enabled = hasSprite;
        cardArtworkImage.sprite = sprite;
        cardArtworkImage.color = hasSprite ? Color.white : Color.clear;
        cardPlaceholderText.gameObject.SetActive(!hasSprite);
        cardPlaceholderText.text = isGameOver ? "结局" : "卡片图片";
        artworkFrame.SetStyle(artworkFillColor, artworkStrokeColor, 20f, 2f);
    }

    private void OnLeftClicked()
    {
        gameManager.ChooseLeft();
    }

    private void OnRightClicked()
    {
        gameManager.ChooseRight();
    }

    private string GetOverallStateText()
    {
        if (gameManager == null)
        {
            return "普通";
        }

        int minValue = PlayerStats.MaxValue;
        foreach (StatType statType in StatDisplayOrder)
        {
            minValue = Mathf.Min(minValue, gameManager.PlayerStats.GetValue(statType));
        }

        if (gameManager.IsGameOver)
        {
            return "结局";
        }

        if (minValue <= 20)
        {
            return "危险";
        }

        if (minValue <= 40)
        {
            return "偏低";
        }

        return "普通";
    }

    private static Font ResolveUIFont()
    {
        string[] preferredFonts =
        {
            "Microsoft YaHei UI",
            "Microsoft YaHei",
            "SimHei",
            "Noto Sans CJK SC",
            "Arial Unicode MS",
            "Arial"
        };

        try
        {
            Font osFont = Font.CreateDynamicFontFromOSFont(preferredFonts, 24);
            if (osFont != null)
            {
                return osFont;
            }
        }
        catch
        {
            // The built-in font keeps the prototype usable if the OS font lookup fails.
        }

        return Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
    }

    private static Sprite LoadEditorBackgroundSprite()
    {
#if UNITY_EDITOR
        return AssetDatabase.LoadAssetAtPath<Sprite>($"{MainGameUIResourceLibraryPath}/Backgrounds/spr_ui_game_bg_warm_main.png");
#else
        return null;
#endif
    }

    private static Sprite LoadStatIconSprite(StatType statType, bool isFillMask)
    {
        string resourcePath = GetStatIconResourcePath(statType, isFillMask);
        Sprite sprite = Resources.Load<Sprite>(resourcePath);
        if (sprite != null)
        {
            return sprite;
        }

#if UNITY_EDITOR
        return AssetDatabase.LoadAssetAtPath<Sprite>($"{MainGameUIResourceLibraryPath}/Resources/{resourcePath}.png");
#else
        return null;
#endif
    }

    private static string GetStatIconResourcePath(StatType statType, bool isFillMask)
    {
        statType = StatTypeUtility.Normalize(statType);
        string fileName;

        switch (statType)
        {
            case StatType.Health:
                fileName = isFillMask ? "icon_body_mind_fill_mask_512" : "icon_body_mind_transparent_512";
                break;
            case StatType.Money:
                fileName = isFillMask ? "icon_money_fill_mask_512" : "icon_money_transparent_512";
                break;
            case StatType.Social:
                fileName = isFillMask ? "icon_social_fill_mask_512" : "icon_social_transparent_512";
                break;
            case StatType.Academic:
                fileName = isFillMask ? "icon_study_fill_mask_512" : "icon_study_transparent_512";
                break;
            default:
                fileName = isFillMask ? "icon_body_mind_fill_mask_512" : "icon_body_mind_transparent_512";
                break;
        }

        return $"UI/Stats/{fileName}";
    }

    private static Vector2 GetStatIconDisplaySize(StatType statType)
    {
        statType = StatTypeUtility.Normalize(statType);

        // Source sprites are 512x512 with different transparent padding.
        // These display boxes normalize visible icon height while preserving each icon's aspect.
        switch (statType)
        {
            case StatType.Health:
            case StatType.Money:
                return new Vector2(160f, 160f);
            case StatType.Social:
                return new Vector2(250f, 250f);
            case StatType.Academic:
                return new Vector2(198f, 198f);
            default:
                return new Vector2(160f, 160f);
        }
    }

    private static string GetStatDisplayName(StatType statType)
    {
        statType = StatTypeUtility.Normalize(statType);

        switch (statType)
        {
            case StatType.Health:
                return "身心";
            case StatType.Money:
                return "经济";
            case StatType.Social:
                return "人际";
            case StatType.Academic:
                return "学业";
            default:
                return statType.ToString();
        }
    }

    private static SimpleIconKind GetStatIcon(StatType statType)
    {
        statType = StatTypeUtility.Normalize(statType);

        switch (statType)
        {
            case StatType.Health:
                return SimpleIconKind.Health;
            case StatType.Money:
                return SimpleIconKind.Money;
            case StatType.Social:
                return SimpleIconKind.Social;
            case StatType.Academic:
                return SimpleIconKind.Academic;
            default:
                return SimpleIconKind.Status;
        }
    }

    private class StatWidget
    {
        private readonly StatType statType;
        private readonly Text labelText;
        private readonly Image fillImage;
        private readonly StatChangeDotGraphic changeDot;
        private readonly Color normalColor;
        private readonly Color previewColor;
        private readonly Color positiveColor;
        private readonly Color negativeColor;
        private readonly float fillAnimationDuration;
        private readonly float colorAnimationDuration;
        private Color animationColor;
        private float fillAnimationTimer;
        private float colorAnimationTimer;
        private float animationStartValue;
        private float displayedValue;
        private int targetValue;
        private bool hasValue;

        public StatWidget(
            StatType statType,
            Text labelText,
            Image fillImage,
            StatChangeDotGraphic changeDot,
            Color normalColor,
            Color previewColor,
            Color positiveColor,
            Color negativeColor,
            float fillAnimationDuration,
            float colorAnimationDuration)
        {
            this.statType = statType;
            this.labelText = labelText;
            this.fillImage = fillImage;
            this.changeDot = changeDot;
            this.normalColor = normalColor;
            this.previewColor = previewColor;
            this.positiveColor = positiveColor;
            this.negativeColor = negativeColor;
            this.fillAnimationDuration = fillAnimationDuration;
            this.colorAnimationDuration = colorAnimationDuration;
        }

        public void Refresh(int value)
        {
            labelText.text = GetStatDisplayName(statType);
            labelText.gameObject.name = $"{statType}Label";

            int clampedValue = Mathf.Clamp(value, PlayerStats.MinValue, PlayerStats.MaxValue);
            if (!hasValue)
            {
                hasValue = true;
                targetValue = clampedValue;
                displayedValue = clampedValue;
                ApplyFill(displayedValue / 100f, normalColor);
                changeDot.Hide();
                return;
            }

            if (clampedValue == targetValue)
            {
                return;
            }

            int changeAmount = clampedValue - targetValue;
            targetValue = clampedValue;
            animationStartValue = displayedValue;
            fillAnimationTimer = fillAnimationDuration;
            colorAnimationTimer = colorAnimationDuration;
            animationColor = changeAmount > 0 ? positiveColor : negativeColor;
            ApplyFill(displayedValue / 100f, animationColor);
            changeDot.Show(changeAmount, positiveColor, negativeColor);
        }

        public void ShowPreview(int effectiveAmount)
        {
            if (fillAnimationTimer > 0f)
            {
                return;
            }

            changeDot.ShowMagnitude(effectiveAmount, previewColor);
        }

        public void ClearPreview()
        {
            if (fillAnimationTimer <= 0f)
            {
                changeDot.Hide();
            }
        }

        public void UpdateAnimation(float deltaTime)
        {
            if (fillAnimationTimer <= 0f && colorAnimationTimer <= 0f)
            {
                return;
            }

            if (fillAnimationTimer > 0f)
            {
                fillAnimationTimer = Mathf.Max(0f, fillAnimationTimer - deltaTime);
                float normalizedFillTime = 1f - fillAnimationTimer / fillAnimationDuration;
                float easedFillTime = Mathf.SmoothStep(0f, 1f, normalizedFillTime);
                displayedValue = Mathf.Lerp(animationStartValue, targetValue, easedFillTime);
            }

            Color nextColor = normalColor;
            if (colorAnimationTimer > 0f)
            {
                colorAnimationTimer = Mathf.Max(0f, colorAnimationTimer - deltaTime);
                float normalizedColorTime = 1f - colorAnimationTimer / colorAnimationDuration;
                float easedColorTime = Mathf.SmoothStep(0f, 1f, normalizedColorTime);
                nextColor = Color.Lerp(animationColor, normalColor, easedColorTime);
            }

            ApplyFill(displayedValue / 100f, nextColor);

            if (fillAnimationTimer <= 0f && colorAnimationTimer <= 0f)
            {
                displayedValue = targetValue;
                ApplyFill(displayedValue / 100f, normalColor);
                changeDot.Hide();
            }
        }

        private void ApplyFill(float normalizedAmount, Color fillColor)
        {
            fillImage.fillAmount = Mathf.Clamp01(normalizedAmount);
            fillImage.color = fillColor;
        }
    }
}
