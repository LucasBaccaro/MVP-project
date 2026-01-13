# Unity UI Toolkit

Unity UI Toolkit is a retained-mode UI system for developing user interfaces in Unity, based on recognized web technologies like HTML, CSS, and XML. It provides a comprehensive framework for creating both runtime game UIs and custom Unity Editor extensions through a declarative, web-inspired approach. The system is built on three core pillars: a hierarchical visual element tree, a stylesheet-based styling system (USS), and an event propagation architecture that supports both trickle-down and bubble-up phases.

The toolkit is available both as a built-in Unity feature and as a package (com.unity.ui v1.0.0-preview.18). The built-in version focuses on Editor UI development, while the package version extends functionality to runtime applications with components like UIDocument and PanelSettings. It supports flexbox layouts through the Yoga layout engine, provides 40+ built-in UI controls, includes data binding capabilities, and offers virtualized scrolling for performance-critical lists and trees.

## Core Visual Element System

### Creating and Manipulating Visual Elements

```csharp
using UnityEngine;
using UnityEngine.UIElements;

public class VisualElementExample : MonoBehaviour
{
    void OnEnable()
    {
        var root = GetComponent<UIDocument>().rootVisualElement;

        // Create elements programmatically
        var container = new VisualElement();
        container.name = "main-container";
        container.AddToClassList("container");

        // Add child elements
        var label = new Label("Hello World");
        label.AddToClassList("title");
        container.Add(label);

        var button = new Button(() => Debug.Log("Clicked!"));
        button.text = "Click Me";
        container.Add(button);

        // Hierarchy manipulation
        root.Add(container);
        root.Insert(0, new Label("First Element"));
        root.Remove(button);
        root.Clear(); // Remove all children

        // Query elements
        var myLabel = root.Q<Label>("labelName");
        var allButtons = root.Query<Button>(className: "game-button").ToList();
        var firstButton = root.Q<Button>(className: "game-button");

        // Modify styles inline
        container.style.backgroundColor = Color.blue;
        container.style.flexGrow = 1;
        container.style.padding = 10;
    }
}
```

### Working with USS Classes and Pseudo-States

```csharp
using UnityEngine.UIElements;

public class ClassManipulationExample : MonoBehaviour
{
    private Button gameButton;
    private const string ActiveClassName = "game-button--active";

    void Setup()
    {
        var root = GetComponent<UIDocument>().rootVisualElement;
        gameButton = root.Q<Button>(className: "game-button");

        // Add/remove classes dynamically
        gameButton.AddToClassList("highlighted");
        gameButton.RemoveFromClassList("disabled");

        // Toggle class
        gameButton.ToggleInClassList("selected");

        // Conditional class application
        gameButton.EnableInClassList("error", hasError);

        // Check class membership
        if (gameButton.ClassListContains(ActiveClassName))
        {
            Debug.Log("Button is active");
        }

        // Picking mode - control whether element receives pointer events
        gameButton.pickingMode = PickingMode.Position; // Default, receives events
        gameButton.pickingMode = PickingMode.Ignore;   // Ignores pointer events

        // Visibility
        gameButton.visible = false; // Element invisible but takes layout space
        gameButton.style.display = DisplayStyle.None; // Element removed from layout
    }
}
```

## UXML - Declarative UI Structure

### Loading and Instantiating UXML

```csharp
using UnityEngine;
using UnityEngine.UIElements;

[RequireComponent(typeof(UIDocument))]
public class UXMLLoadingExample : MonoBehaviour
{
    [SerializeField] private VisualTreeAsset sourceAsset;
    [SerializeField] private PanelSettings panelSettings;

    void Awake()
    {
        var uiDocument = GetComponent<UIDocument>();
        uiDocument.panelSettings = panelSettings;
        uiDocument.visualTreeAsset = sourceAsset;
    }

    void OnEnable()
    {
        var root = GetComponent<UIDocument>().rootVisualElement;

        // Query elements defined in UXML
        var headerLabel = root.Q<Label>(className: "header-title");
        var gameButtons = root.Query<Button>(className: "game-button").ToList();

        // Clone additional UXML into existing tree
        var additionalAsset = Resources.Load<VisualTreeAsset>("Layouts/Panel");
        additionalAsset.CloneTree(root);
    }
}
```

### UXML File Structure

```xml
<UXML xmlns="UnityEngine.UIElements">
    <VisualElement class="main-container">
        <VisualElement class="header">
            <Label class="header-title" text="Whack-A-Button" />
        </VisualElement>
        <VisualElement class="score">
            <Label class="score-label" text="Score: " />
            <Label class="score-label score-number" text="0" />
        </VisualElement>
        <VisualElement class="board">
            <VisualElement class="board__row">
                <Button class="game-button" />
                <Button class="game-button" />
            </VisualElement>
            <VisualElement class="board__row">
                <Button class="game-button" />
                <Button class="game-button" />
            </VisualElement>
        </VisualElement>
        <TextField binding-path="playerName" label="Name:" />
        <Toggle binding-path="isEnabled" label="Enabled" />
    </VisualElement>
</UXML>
```

## USS - Styling System

### Loading and Applying Stylesheets

```csharp
using UnityEngine;
using UnityEngine.UIElements;
#if UNITY_EDITOR
using UnityEditor;
#endif

public class StyleSheetExample : MonoBehaviour
{
    [SerializeField] private StyleSheet styleSheet;

    void OnEnable()
    {
        var root = GetComponent<UIDocument>().rootVisualElement;

        // Apply stylesheet at runtime
        root.styleSheets.Add(styleSheet);

#if UNITY_EDITOR
        // Load stylesheet in Editor
        var editorSheet = AssetDatabase.LoadAssetAtPath<StyleSheet>(
            "Assets/Styles/editor-styles.uss"
        );
        root.styleSheets.Add(editorSheet);
#endif
    }
}
```

### USS File Syntax

```css
/* Main container */
.main-container {
    background-color: white;
    flex-grow: 1;
}

/* Header styling */
.header {
    align-items: center;
    background-color: midnightblue;
    margin: 10px;
    padding: 10px;
}

.header-title {
    color: white;
    font-size: 48px;
}

/* Score display */
.score {
    background-color: midnightblue;
    flex-direction: row;
    justify-content: center;
    margin: 10px;
    padding: 10px;
}

.score-label {
    color: white;
    font-size: 36px;
    white-space: nowrap;
}

/* Game board layout */
.board {
    flex-grow: 1;
    margin: 10px;
}

.board__row {
    flex-direction: row;
    flex-grow: 1;
}

/* Button states with pseudo-classes */
.game-button {
    background-color: dimgray;
    flex-grow: 1;
    margin: 10px;
}

.game-button:hover {
    border-radius: 15px;
    background-color: gray;
}

.game-button:active {
    border-radius: 15px;
    background-color: darkgray;
}

.game-button--active {
    background-color: darkolivegreen;
}

.game-button--active:hover {
    background-color: lime;
}

/* Disabled state */
Button:disabled {
    opacity: 0.5;
}
```

## Event System

### Mouse and Click Events

```csharp
using UnityEngine;
using UnityEngine.UIElements;

public class ClickEventExample : MonoBehaviour
{
    private Label scoreLabel;
    private int score = 0;
    private const string ActiveClassName = "game-button--active";

    void OnEnable()
    {
        var root = GetComponent<UIDocument>().rootVisualElement;
        scoreLabel = root.Q<Label>(className: "score-number");

        var gameBoard = root.Q<VisualElement>(className: "board");

        // Register click event with event bubbling
        gameBoard.RegisterCallback<ClickEvent>(evt =>
        {
            if (evt.target is Button targetButton &&
                targetButton.ClassListContains(ActiveClassName))
            {
                score++;
                scoreLabel.text = score.ToString();
                targetButton.RemoveFromClassList(ActiveClassName);
                evt.StopImmediatePropagation();
            }
        });

        // Individual button click with Action callback
        var button = root.Q<Button>("myButton");
        button.clicked += OnButtonClicked;
    }

    void OnButtonClicked()
    {
        Debug.Log("Button clicked!");
    }
}
```

### Pointer Events with Capture

```csharp
using UnityEngine.UIElements;

public class PointerEventsExample : MonoBehaviour
{
    private VisualElement mainArea;
    private Label tooltip;

    void OnEnable()
    {
        var root = GetComponent<UIDocument>().rootVisualElement;
        mainArea = root.Q<VisualElement>(className: "main-area");
        tooltip = root.Q<Label>(className: "tooltip");

        // Register pointer event handlers
        mainArea.RegisterCallback<PointerDownEvent>(OnPointerDown);
        mainArea.RegisterCallback<PointerUpEvent>(OnPointerUp);
        mainArea.RegisterCallback<PointerMoveEvent>(OnPointerMove);
        mainArea.RegisterCallback<PointerEnterEvent>(OnPointerEnter);
        mainArea.RegisterCallback<PointerLeaveEvent>(OnPointerLeave);
    }

    void OnPointerDown(PointerDownEvent evt)
    {
        tooltip.text = $"Pointer Down at {evt.localPosition}";
        mainArea.AddToClassList("active");

        // Capture pointer to receive events even when outside element
        mainArea.CapturePointer(evt.pointerId);
        evt.StopPropagation();
    }

    void OnPointerUp(PointerUpEvent evt)
    {
        tooltip.text = "Pointer Up";
        mainArea.RemoveFromClassList("active");
        mainArea.ReleasePointer(evt.pointerId);
    }

    void OnPointerMove(PointerMoveEvent evt)
    {
        // Only handle if we have pointer captured
        if (mainArea.HasPointerCapture(evt.pointerId))
        {
            tooltip.style.left = evt.localPosition.x;
            tooltip.style.top = evt.localPosition.y;
        }
    }

    void OnPointerEnter(PointerEnterEvent evt)
    {
        mainArea.style.backgroundColor = Color.yellow;
    }

    void OnPointerLeave(PointerLeaveEvent evt)
    {
        mainArea.style.backgroundColor = Color.white;
    }
}
```

### Keyboard Events

```csharp
using UnityEngine.UIElements;

public class KeyboardEventExample : MonoBehaviour
{
    void OnEnable()
    {
        var root = GetComponent<UIDocument>().rootVisualElement;
        var textField = root.Q<TextField>();

        // Register keyboard callbacks
        textField.RegisterCallback<KeyDownEvent>(OnKeyDown);
        textField.RegisterCallback<KeyUpEvent>(OnKeyUp);

        // Set focus to receive keyboard events
        textField.Focus();
    }

    void OnKeyDown(KeyDownEvent evt)
    {
        Debug.Log($"Key pressed: {evt.keyCode}, Character: {evt.character}");

        if (evt.keyCode == KeyCode.Return)
        {
            Debug.Log("Enter key pressed");
            evt.StopPropagation();
            evt.PreventDefault();
        }

        // Check modifiers
        if (evt.ctrlKey)
        {
            Debug.Log("Control key held");
        }

        if (evt.shiftKey && evt.keyCode == KeyCode.S)
        {
            Debug.Log("Shift+S pressed");
        }
    }

    void OnKeyUp(KeyUpEvent evt)
    {
        Debug.Log($"Key released: {evt.keyCode}");
    }
}
```

### Value Change Events

```csharp
using UnityEngine.UIElements;

public class ValueChangeExample : MonoBehaviour
{
    void OnEnable()
    {
        var root = GetComponent<UIDocument>().rootVisualElement;

        // TextField change event
        var textField = root.Q<TextField>("nameField");
        textField.RegisterValueChangedCallback(evt =>
        {
            Debug.Log($"Text changed from '{evt.previousValue}' to '{evt.newValue}'");
        });

        // Toggle change event
        var toggle = root.Q<Toggle>("enabledToggle");
        toggle.RegisterValueChangedCallback(evt =>
        {
            Debug.Log($"Toggle changed: {evt.newValue}");
        });

        // Slider change event
        var slider = root.Q<Slider>("volumeSlider");
        slider.RegisterValueChangedCallback(evt =>
        {
            Debug.Log($"Volume: {evt.newValue}");
        });

        // Set value without triggering callback
        textField.SetValueWithoutNotify("New Value");
    }
}
```

## Common UI Controls

### Button and Interactive Elements

```csharp
using UnityEngine;
using UnityEngine.UIElements;

public class ButtonExample : MonoBehaviour
{
    void OnEnable()
    {
        var root = GetComponent<UIDocument>().rootVisualElement;

        // Create button with callback
        var button = new Button(() => Debug.Log("Clicked!"));
        button.text = "Click Me";
        root.Add(button);

        // Create button without callback
        var button2 = new Button();
        button2.text = "Another Button";
        button2.clicked += OnButton2Clicked;
        root.Add(button2);

        // Toggle
        var toggle = new Toggle("Enable Feature");
        toggle.value = true;
        toggle.RegisterValueChangedCallback(evt =>
        {
            Debug.Log($"Feature enabled: {evt.newValue}");
        });
        root.Add(toggle);

        // Radio buttons
        var radioGroup = new RadioButtonGroup("Options", new[] { "Option A", "Option B", "Option C" });
        radioGroup.value = 0;
        radioGroup.RegisterValueChangedCallback(evt =>
        {
            Debug.Log($"Selected option: {evt.newValue}");
        });
        root.Add(radioGroup);
    }

    void OnButton2Clicked()
    {
        Debug.Log("Button 2 clicked!");
    }
}
```

### Text Input Fields

```csharp
using UnityEngine.UIElements;

public class TextFieldExample : MonoBehaviour
{
    void OnEnable()
    {
        var root = GetComponent<UIDocument>().rootVisualElement;

        // Single-line text field
        var textField = new TextField("Player Name");
        textField.value = "Player1";
        textField.maxLength = 20;
        textField.RegisterValueChangedCallback(evt =>
        {
            Debug.Log($"Name changed to: {evt.newValue}");
        });
        root.Add(textField);

        // Multi-line text field
        var multilineField = new TextField("Description");
        multilineField.multiline = true;
        root.Add(multilineField);

        // Password field (masks characters)
        var passwordField = new TextField("Password");
        passwordField.isPasswordField = true;
        root.Add(passwordField);

        // Dropdown field
        var dropdown = new DropdownField("Difficulty",
            new[] { "Easy", "Medium", "Hard" },
            0);
        dropdown.RegisterValueChangedCallback(evt =>
        {
            Debug.Log($"Selected: {evt.newValue}");
        });
        root.Add(dropdown);
    }
}
```

### Sliders and Progress Bars

```csharp
using UnityEngine;
using UnityEngine.UIElements;

public class SliderExample : MonoBehaviour
{
    void OnEnable()
    {
        var root = GetComponent<UIDocument>().rootVisualElement;

        // Float slider
        var slider = new Slider("Volume", 0f, 100f);
        slider.value = 75f;
        slider.RegisterValueChangedCallback(evt =>
        {
            Debug.Log($"Volume: {evt.newValue}%");
        });
        root.Add(slider);

        // Integer slider
        var intSlider = new SliderInt("Level", 1, 10);
        intSlider.value = 5;
        root.Add(intSlider);

        // Min-max slider (range selection)
        var minMaxSlider = new MinMaxSlider("Range", 0f, 100f, 25f, 75f);
        minMaxSlider.RegisterValueChangedCallback(evt =>
        {
            Debug.Log($"Range: {evt.newValue.x} - {evt.newValue.y}");
        });
        root.Add(minMaxSlider);

        // Progress bar
        var progressBar = new ProgressBar();
        progressBar.title = "Loading...";
        progressBar.lowValue = 0f;
        progressBar.highValue = 100f;
        progressBar.value = 50f;
        root.Add(progressBar);
    }
}
```

### ListView with Virtualization

```csharp
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

public class ListViewExample : MonoBehaviour
{
    private List<string> items;

    void OnEnable()
    {
        var root = GetComponent<UIDocument>().rootVisualElement;

        // Create data source
        items = new List<string>();
        for (int i = 1; i <= 1000; i++)
        {
            items.Add($"Item {i}");
        }

        // Create ListView with virtualization
        Func<VisualElement> makeItem = () => new Label();

        Action<VisualElement, int> bindItem = (element, index) =>
        {
            (element as Label).text = items[index];
        };

        const int itemHeight = 20;
        var listView = new ListView(items, itemHeight, makeItem, bindItem);

        // Configure ListView
        listView.selectionType = SelectionType.Multiple;
        listView.showBorder = true;
        listView.showAlternatingRowBackgrounds = AlternatingRowBackground.All;
        listView.style.flexGrow = 1;

        // Handle selection
        listView.onSelectionChange += selectedItems =>
        {
            Debug.Log($"Selected {selectedItems.Count()} items");
        };

        // Handle item activation (double-click)
        listView.onItemsChosen += selectedItems =>
        {
            foreach (var item in selectedItems)
            {
                Debug.Log($"Activated: {item}");
            }
        };

        root.Add(listView);
    }
}
```

## Data Binding

### Binding UI to Serialized Properties

```csharp
using UnityEngine;
using UnityEngine.UIElements;

public class DataBindingExample : MonoBehaviour
{
    [SerializeField] private string playerName = "Player1";
    [SerializeField] private bool isEnabled = true;
    [SerializeField] private float health = 100f;
    [SerializeField] private int level = 1;

    void OnEnable()
    {
        var root = GetComponent<UIDocument>().rootVisualElement;

        // Query fields with binding-path set in UXML
        var nameField = root.Q<TextField>(name: "playerNameField");
        var enabledToggle = root.Q<Toggle>(name: "enabledToggle");
        var healthSlider = root.Q<Slider>(name: "healthSlider");

        // Manually bind if not using UXML binding-path
        nameField.bindingPath = "playerName";
        enabledToggle.bindingPath = "isEnabled";
        healthSlider.bindingPath = "health";

        // Programmatic binding alternative
        nameField.RegisterValueChangedCallback(evt =>
        {
            playerName = evt.newValue;
        });
    }
}
```

### INotifyValueChanged Interface

```csharp
using UnityEngine;
using UnityEngine.UIElements;

public class ValueChangeNotificationExample : MonoBehaviour
{
    void OnEnable()
    {
        var root = GetComponent<UIDocument>().rootVisualElement;

        var textField = root.Q<TextField>();

        // Get current value
        string currentValue = textField.value;

        // Set value and trigger ChangeEvent
        textField.value = "New Value";

        // Set value without triggering event
        textField.SetValueWithoutNotify("Silent Update");

        // Register callback
        textField.RegisterValueChangedCallback(evt =>
        {
            Debug.Log($"Changed from '{evt.previousValue}' to '{evt.newValue}'");
        });
    }
}
```

## Scheduling and Animation

### Timer and Scheduled Callbacks

```csharp
using UnityEngine;
using UnityEngine.UIElements;

public class SchedulerExample : MonoBehaviour
{
    private Label timerLabel;
    private float elapsedTime;

    void OnEnable()
    {
        var root = GetComponent<UIDocument>().rootVisualElement;
        timerLabel = root.Q<Label>("timer");

        // Execute once after delay
        root.schedule.Execute(() =>
        {
            Debug.Log("Executed after 2 seconds");
        }).StartingIn(2000); // milliseconds

        // Execute repeatedly
        root.schedule.Execute(() =>
        {
            elapsedTime += 0.016f;
            timerLabel.text = $"Time: {elapsedTime:F2}";
        }).Every(16); // Every 16ms (~60 FPS)

        // Execute for limited duration
        root.schedule.Execute(() =>
        {
            Debug.Log("Tick");
        }).Every(100).For(5000); // Every 100ms for 5 seconds

        // Execute until condition
        int counter = 0;
        root.schedule.Execute(() =>
        {
            counter++;
            Debug.Log($"Count: {counter}");
        }).Every(500).Until(() => counter >= 10);
    }
}
```

### Transitioning Styles

```csharp
using UnityEngine;
using UnityEngine.UIElements;

public class StyleTransitionExample : MonoBehaviour
{
    void OnEnable()
    {
        var root = GetComponent<UIDocument>().rootVisualElement;
        var box = root.Q<VisualElement>("animatedBox");

        // Animate opacity
        float targetOpacity = 0f;
        float duration = 1000f; // milliseconds
        float startTime = Time.realtimeSinceStartup * 1000f;

        box.schedule.Execute(() =>
        {
            float elapsed = (Time.realtimeSinceStartup * 1000f) - startTime;
            float t = Mathf.Clamp01(elapsed / duration);
            box.style.opacity = Mathf.Lerp(1f, targetOpacity, t);
        }).Every(16).Until(() => box.style.opacity.value <= targetOpacity);
    }
}
```

## Complete Runtime Game UI Example

### Whack-A-Button Game Implementation

```csharp
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

[RequireComponent(typeof(UIDocument))]
public class WhackAButtonGame : MonoBehaviour
{
    private enum GameState { Waiting, Active }

    private const string ActiveClassName = "game-button--active";

    [SerializeField] private PanelSettings panelSettings;
    [SerializeField] private VisualTreeAsset sourceAsset;
    [SerializeField] private StyleSheet styleSheet;

    private List<Button> gameButtons;
    private Label scoreLabel;
    private GameState currentState = GameState.Waiting;
    private int activeButtonIndex = -1;
    private float delay = 3f;
    private int score;

    void Awake()
    {
        var uiDocument = GetComponent<UIDocument>();
        uiDocument.panelSettings = panelSettings;
        uiDocument.visualTreeAsset = sourceAsset;
    }

    void OnEnable()
    {
        if (scoreLabel == null)
        {
            score = 0;
            InitializeVisualTree(GetComponent<UIDocument>());
        }
    }

    private void InitializeVisualTree(UIDocument doc)
    {
        var root = doc.rootVisualElement;

        // Query UI elements
        scoreLabel = root.Q<Label>(className: "score-number");
        scoreLabel.text = score.ToString();

        gameButtons = root.Query<Button>(className: "game-button").ToList();

        // Register click handler on parent (event bubbling)
        var gameBoard = root.Q<VisualElement>(className: "board");
        gameBoard.RegisterCallback<ClickEvent>(evt =>
        {
            if (evt.target is Button targetButton &&
                targetButton.ClassListContains(ActiveClassName))
            {
                score++;
                scoreLabel.text = score.ToString();
                targetButton.RemoveFromClassList(ActiveClassName);
                evt.StopImmediatePropagation();
            }
        });

        // Apply stylesheet
        root.styleSheets.Add(styleSheet);
    }

    void Update()
    {
        delay -= Time.deltaTime;

        if (delay < 0f)
        {
            switch (currentState)
            {
                case GameState.Waiting:
                    activeButtonIndex = Random.Range(0, gameButtons.Count);
                    gameButtons[activeButtonIndex].AddToClassList(ActiveClassName);
                    currentState = GameState.Active;
                    delay = Random.Range(0.5f, 2f);
                    break;

                case GameState.Active:
                    gameButtons[activeButtonIndex].RemoveFromClassList(ActiveClassName);
                    currentState = GameState.Waiting;
                    delay = Random.Range(1f, 4f);
                    break;
            }
        }
    }
}
```

## Summary

Unity UI Toolkit serves two primary use cases: creating runtime user interfaces for games and applications, and developing custom Unity Editor extensions. For runtime UI, developers use the UIDocument component with PanelSettings to instantiate UXML-defined interfaces, style them with USS, and add interactivity through the event system. The virtualized ListView and TreeView controls enable efficient display of large datasets, while the data binding system connects UI elements directly to serialized properties. The retained-mode architecture means UI updates are handled declaratively rather than requiring per-frame rendering code, making it ideal for HUDs, menus, inventories, and debug overlays.

Integration patterns follow a separation of concerns: UXML files define structure, USS files handle presentation, and C# scripts manage behavior and state. The query system (Q and Query methods) provides fast element lookups, while the event propagation model supports both capturing (trickle-down) and bubbling phases for flexible event handling. The scheduler API enables time-based animations and callbacks without manual Update loop management. For Editor extensions, UI Toolkit replaces IMGUI with a modern, performant alternative that supports complex layouts, custom inspectors, and tool windows. The system's web-inspired approach reduces the learning curve for developers familiar with HTML/CSS while providing Unity-specific optimizations like the Yoga layout engine and batch rendering for maximum performance.
