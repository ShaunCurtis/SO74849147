To answer this question I need a Modal Dialog Framework.  I've included a simple one as an appedix to this answer, but you can use any of those available that are suitable.

First a "Text Editor" component which we will use in the modal dialog:

### TextEditor.razor

```csharp
<div class="mb-3">
    <label class="form-label small text-muted">@this.Label</label>
    <div class="input-group">
        <input class="form-control" value="@this.Value" @oninput=this.Changed />
        <button class="btn btn-success" @onclick=Exit>Set</button>
        <button class="btn btn-danger" @onclick=Cancel>Cancel</button>
    </div>
</div>

@code {
    [CascadingParameter] private IModalDialog? Modal { get; set; }
    [Parameter] public string Label { get; set; } = "Field";
    [Parameter] public string? Value { get; set; }
    [Parameter] public EventCallback<string> ValueChanged { get; set; }
    [Parameter] public Expression<Func<string>>? ValueExpression { get; set; }

    private string? _value;

    protected override void OnInitialized()
        => _value = Value;

    private void Changed(ChangeEventArgs e)
    => this.ValueChanged.InvokeAsync(e.Value?.ToString() ?? null);

    private void Exit()
        => Modal?.Close(ModalResult.OK());

    private async Task Cancel()
    {
        await this.ValueChanged.InvokeAsync(_value);
        Modal?.Close(ModalResult.Cancel());
    }
}
```

And then our "Special Format" page:

```csharp
@page "/"

<PageTitle>Index</PageTitle>

<h1 class="mb-5">Hello, world!</h1>

<div class="mt-5 mb-5" >
    Editor
</div>

<div class="mt-5 border border-1 border-dark p-1" style="cursor:pointer;" @onclick="() => Edit(CountryModalOptions)">
    @model.Country
</div>

<div class="mt-2 border border-1 border-dark p-1" style="cursor:pointer;" @onclick="() => Edit(ContinentModalOptions)">
    @model.Continent
</div>

<BaseModalDialog @ref=this.Modal />

@code {
    private IModalDialog? Modal;
    private Model model = new();

    private async Task Edit(ModalOptions options)
    {
        if (Modal is not null)
        {
            var result = await Modal.ShowAsync<TextEditor>(options);
            // do things
        }
    }

    private ModalOptions CountryModalOptions
    {
        get
        {
            var options = new ModalOptions();
            options.ControlParameters.Add("Label", "Field Name");
            options.ControlParameters.Add("Value", model.Country);
            options.ControlParameters.Add("ValueChanged", EventCallback.Factory.Create<String>(this, RuntimeHelpers.CreateInferredEventCallback(this, __value => model.Country = __value, model.Country)));
            options.ControlParameters.Add("ValueExpression", RuntimeHelpers.TypeCheck<System.Linq.Expressions.Expression<System.Func<System.String>>>(() => model.Country));
            return options;
        }
    }

    private ModalOptions ContinentModalOptions
    {
        get
        {
            var options = new ModalOptions();
            options.ControlParameters.Add("Label", "Field Name");
            options.ControlParameters.Add("Value", model.Continent);
            options.ControlParameters.Add("ValueChanged", EventCallback.Factory.Create<String>(this, RuntimeHelpers.CreateInferredEventCallback(this, __value => model.Continent = __value, model.Continent)));
            options.ControlParameters.Add("ValueExpression", RuntimeHelpers.TypeCheck<System.Linq.Expressions.Expression<System.Func<System.String>>>(() => model.Continent));
            return options;
        }
    }

    public class Model
    {
        public string Country { get; set; } = "No Country Set";
        public string Continent { get; set; } = "No Continent Set";
    }
}
```

So what's going on.

We have our `BaseModalDialog` component registered on the page with a global variable reference.  When you click on an editable field the onclick handler opens the modal dialog by calling `ShowAsync`.  It sets the component as `T` and passes in a set of Parameters to apply to `T`.  The standard bind Parameters are mapped to the model field using some Runtime Helpers.

The dialog opens and you can edit the value.  I've wired it up to update on each keypress (oninput).  You can see thew value changing in the unbderlying page.  Click on either `Set` or `Cancel` to exit the model as pass "control" back to the main page.

I've kept everything as basic as I can, there are all sorts of Css and formating options you can apply to improve the UX.

# Appendix The Modal Dialog

## ModalOptions


```csharp
public sealed class ModalOptions : IEnumerable<KeyValuePair<string, object>>
{
    public string Width { get; set; } = "50%";
    public Dictionary<string, object> ControlParameters { get; } = new Dictionary<string, object>();

    public IEnumerator<KeyValuePair<string, object>> GetEnumerator()
    {
        foreach (var item in ControlParameters)
            yield return item;
    }

    IEnumerator IEnumerable.GetEnumerator()
        => this.GetEnumerator();

    public T? Get<T>(string key)
    {
        if (this.ControlParameters.ContainsKey(key))
        {
            if (this.ControlParameters[key] is T t) return t;
        }
        return default;
    }

    public bool TryGet<T>(string key, [NotNullWhen(true)] out T? value)
    {
        value = default;
        if (this.ControlParameters.ContainsKey(key))
        {
            if (this.ControlParameters[key] is T t)
            {
                value = t;
                return true;
            }
        }
        return false;
    }

    public bool Set(string key, object value)
    {
        if (this.ControlParameters.ContainsKey(key))
        {
            this.ControlParameters[key] = value;
            return false;
        }
        this.ControlParameters.Add(key, value);
        return true;
    }
}
```

## ModalResult

```csharp
public enum ModalResultType { NoSet, OK, Cancel, Exit }

public sealed class ModalResult
{
    public ModalResultType ResultType { get; private set; } = ModalResultType.NoSet;
    public object? Data { get; set; } = null;

    public static ModalResult OK() => new ModalResult() { ResultType = ModalResultType.OK };
    public static ModalResult Exit() => new ModalResult() { ResultType = ModalResultType.Exit };
    public static ModalResult Cancel() => new ModalResult() { ResultType = ModalResultType.Cancel };
    public static ModalResult OK(object data) => new ModalResult() { Data = data, ResultType = ModalResultType.OK };
    public static ModalResult Exit(object data) => new ModalResult() { Data = data, ResultType = ModalResultType.Exit };
    public static ModalResult Cancel(object data) => new ModalResult() { Data = data, ResultType = ModalResultType.Cancel };
}
```

## IModalDialog

```csharp
public interface IModalDialog
{
    public ModalOptions Options { get; }
    public bool IsActive { get; }
    public bool Display { get; }
    public Task<ModalResult> ShowAsync<TModal>(ModalOptions options) where TModal : IComponent;
    public void Dismiss();
    public void Close(ModalResult result);
    public void Update(ModalOptions? options = null);
}
```

## BaseModalDialog.razor

```csharp
@namespace Blazr.UI
@implements IModalDialog

@if (this.Display)
{
    <CascadingValue Value="(IModalDialog)this">
        <div class="base-modal-background">
            <div class="base-modal-content" style="width:@this.Options.Width;" @onclick:stopPropagation="true">
                <DynamicComponent Type=this.ModalContentType Parameters=this.Options.ControlParameters />
            </div>
        </div>
    </CascadingValue>
}

@code {
    public ModalOptions Options { get; protected set; } = new ModalOptions();
    public bool Display { get; protected set; }
    public bool IsActive => this.ModalContentType is not null;
    protected TaskCompletionSource<ModalResult> _ModalTask { get; set; } = new TaskCompletionSource<ModalResult>();
    protected Type? ModalContentType = null;

    public Task<ModalResult> ShowAsync<TModal>(ModalOptions options) where TModal : IComponent
    {
        this.ModalContentType = typeof(TModal);
        this.Options = options ??= this.Options;
        this._ModalTask = new TaskCompletionSource<ModalResult>();
        this.Display = true;
        InvokeAsync(StateHasChanged);
        return this._ModalTask.Task;
    }

    public void Update(ModalOptions? options = null)
    {
        this.Options = options ??= this.Options;
        InvokeAsync(StateHasChanged);
    }

    public async void Dismiss()
    {
        this._ModalTask.TrySetResult(ModalResult.Cancel());
        await Reset();
    }

    public async void Close(ModalResult result)
    {
        this._ModalTask.TrySetResult(result);
        await Reset();
    }

    private async Task Reset()
    {
        this.Display = false;
        this.ModalContentType = null;
        await InvokeAsync(StateHasChanged);
    }
}
```

## BaseModalDialog.razor.css

```css
div.base-modal-background {
    display: block;
    position: fixed;
    z-index: 101; /* Sit on top */
    left: 0;
    top: 0;
    width: 100%; /* Full width */
    height: 100%; /* Full height */
    overflow: auto; /* Enable scroll if needed */
    background-color: rgb(0,0,0); /* Fallback color */
    background-color: rgba(0,0,0,0.4); /* Black w/ opacity */
}

div.base-modal-content {
    background-color: #fefefe;
    margin: 10% auto;
    padding: 10px;
    border: 2px solid #888;
    width: 50%;
}

```
