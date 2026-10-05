using System.Reflection;
using CorporateStarter.Client.Core.Tables;
using CorporateStarter.Client.UI.Components.Tables;
using CorporateStarter.Shared.Common;
using CorporateStarter.Shared.Dtos.MasterData.Positions;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.RenderTree;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.JSInterop;
using MudBlazor.Services;
using Xunit;

// Exercise the actual Razor component and its lifecycle without a browser or backend.
#pragma warning disable BL0006
namespace CorporateStarter.Tests.Client;

public sealed class TableComponentTests
{
    public enum Column { Name, Description, Status }

    [Fact]
    public async Task Shared_component_renders_positions_and_retains_selection_after_sort()
    {
        var client = new FakeTableClient();
        await using var services = CreateServices();
        await using var renderer = new TestRenderer(services);
        var table = await Mount(renderer, client);
        await renderer.Dispatcher.InvokeAsync(async () =>
        {
            Invoke(table, "SelectRow", client.Items[1], new MouseEventArgs());
            await (Task)Invoke(table, "SortBy", Column.Name)!;
        });
        Assert.Equal(client.Items[1].Id, client.LastFind!.LocateId);
        Assert.Equal("name", Assert.Single(client.LastQuery!.Sorts).Field);
        Assert.Equal("asc", client.LastQuery.Sorts[0].Direction);
        Assert.Equal(client.Items[1].Id, Assert.Single(Read<IReadOnlySet<Guid>>(table, "_selectedIds")));
        Assert.Equal("reference-row-selected", Invoke(table, "RowClass", client.Items[1], 1));
        Assert.Empty(renderer.Errors);
    }

    [Fact]
    public async Task Query_change_drops_missing_selection_and_can_select_first_visible_row()
    {
        var client = new FakeTableClient();
        await using var services = CreateServices();
        await using var renderer = new TestRenderer(services);
        var table = await Mount(renderer, client);
        await renderer.Dispatcher.InvokeAsync(async () =>
        {
            Invoke(table, "SelectRow", client.Items[1], new MouseEventArgs());
            client.Visible = [client.Items[0]];
            client.LocateMissing = true;
            await (Task)Invoke(table, "ReloadAfterQueryChangeAsync", true)!;
        });
        Assert.Equal(client.Items[0].Id, Assert.Single(Read<IReadOnlySet<Guid>>(table, "_selectedIds")));
        Assert.Empty(renderer.Errors);
    }

    [Fact]
    public async Task Read_only_table_keeps_selection_and_sort_but_blocks_all_mutations()
    {
        var client = new FakeTableClient();
        await using var services = CreateServices();
        await using var renderer = new TestRenderer(services);
        var table = await Mount(renderer, client, readOnly: true);
        var editorCalls = 0;
        await renderer.Dispatcher.InvokeAsync(async () =>
        {
            await table.SetParametersAsync(ParameterView.FromDictionary(new Dictionary<string, object?>
            {
                ["OpenEditor"] = new Func<Guid?, Task<MudBlazor.IDialogReference>>(_ =>
                {
                    editorCalls++;
                    throw new InvalidOperationException("A read-only table opened an editor.");
                })
            }));
            // Even an incorrectly permissive capability response cannot enable mutations.
            table.GetType().GetField("_capabilities", BindingFlags.Instance | BindingFlags.NonPublic)!
                .SetValue(table, new TableCapabilities(true, true, true, true, true));
            Invoke(table, "SelectRow", client.Items[1], new MouseEventArgs());
            Assert.False(Read<bool>(table, "CanEdit"));
            Assert.False(Read<bool>(table, "CanDelete"));
            await (Task)Invoke(table, "OpenCreateAsync")!;
            await (Task)Invoke(table, "OpenEditAsync")!;
            await (Task)Invoke(table, "OpenDeleteAsync")!;
            await (Task)Invoke(table, "OnRowDoubleClickAsync", client.Items[1], new MouseEventArgs())!;
            await (Task)Invoke(table, "SortBy", Column.Name)!;
        });
        Assert.Equal(0, editorCalls);
        Assert.Equal(client.Items[1].Id, Assert.Single(Read<IReadOnlySet<Guid>>(table, "_selectedIds")));
        Assert.Equal("name", Assert.Single(client.LastQuery!.Sorts).Field);
        Assert.Empty(renderer.Errors);
    }

    internal static ServiceProvider CreateServices(Action<IServiceCollection>? configure = null)
    {
        var services = new ServiceCollection();
        services.AddSingleton<ILoggerFactory>(NullLoggerFactory.Instance);
        services.AddSingleton(typeof(ILogger<>), typeof(NullLogger<>));
        services.AddMudServices();
        services.AddSingleton<IJSRuntime, NoBrowser>();
        services.AddSingleton<NavigationManager, TestNavigation>();
        configure?.Invoke(services);
        return services.BuildServiceProvider();
    }

    private static async Task<ReferenceTable<PositionListItemDto, Column>> Mount(TestRenderer renderer, FakeTableClient client, bool readOnly = false)
    {
        var parameters = new Dictionary<string, object?>
        {
            ["Client"] = client,
            ["ReadOnly"] = readOnly,
            ["Columns"] = new TableColumn<PositionListItemDto, Column>[]
            {
                new(Column.Name, "name", "Name", x => x.Name),
                new(Column.Description, "description", "Description", x => x.Description),
                new(Column.Status, "isActive", "Status", x => x.IsActive ? "Active" : "Inactive", true)
            },
            ["ItemId"] = new Func<PositionListItemDto, Guid>(x => x.Id),
            ["ItemName"] = new Func<PositionListItemDto, string>(x => x.Name),
            ["Title"] = "Positions"
        };
        return await renderer.Dispatcher.InvokeAsync(() => renderer.Mount<ReferenceTable<PositionListItemDto, Column>>(parameters));
    }
    private static object? Invoke(object target, string name, params object[] args) =>
        target.GetType().GetMethod(name, BindingFlags.Instance | BindingFlags.NonPublic)!.Invoke(target, args);
    private static T Read<T>(object target, string name) =>
        (T)target.GetType().GetProperty(name, BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(target)!;

    private sealed class FakeTableClient : ITableClient<PositionListItemDto>
    {
        public PositionListItemDto[] Items { get; } =
        [new() { Id = Guid.NewGuid(), Name = "Alpha", IsActive = true }, new() { Id = Guid.NewGuid(), Name = "Beta", IsActive = false }];
        public IReadOnlyList<PositionListItemDto>? Visible { get; set; }
        public bool LocateMissing { get; set; }
        public TableRequest? LastQuery { get; private set; }
        public TableFindRequest? LastFind { get; private set; }
        public Task<TableCapabilities> GetCapabilitiesAsync(CancellationToken ct = default) => Task.FromResult(new TableCapabilities(true, true, true, true, true));
        public Task<PagedResult<PositionListItemDto>> QueryAsync(TableRequest request, CancellationToken ct = default)
        {
            LastQuery = request;
            var items = Visible ?? Items;
            return Task.FromResult(new PagedResult<PositionListItemDto> { Items = items.ToList(), Page = 1, PageSize = request.PageSize, TotalCount = items.Count });
        }
        public Task<TableFindResult> FindAsync(TableFindRequest request, CancellationToken ct = default)
        {
            LastFind = request;
            return Task.FromResult(LocateMissing ? new TableFindResult() : new() { Id = request.LocateId, PageNumber = 1 });
        }
        public Task<string[]> FilterValuesAsync(TableValuesRequest request, CancellationToken ct = default) => Task.FromResult(new[] { "Active", "Inactive" });
        public Task DeleteAsync(Guid id, CancellationToken ct = default) => throw new NotSupportedException();
    }
    internal sealed class TestRenderer : Renderer
    {
        public TestRenderer(IServiceProvider services) : base(services, services.GetRequiredService<ILoggerFactory>())
        {
            ElementReferenceContext = new WebElementReferenceContext(services.GetRequiredService<IJSRuntime>());
        }
        public override Dispatcher Dispatcher { get; } = Dispatcher.CreateDefault();
        public List<Exception> Errors { get; } = [];
        protected override void HandleException(Exception exception) => Errors.Add(exception);
        protected override Task UpdateDisplayAsync(in RenderBatch renderBatch) => Task.CompletedTask;
        public async Task<T> Mount<T>(Dictionary<string, object?> parameters) where T : IComponent
        {
            var component = (T)InstantiateComponent(typeof(T));
            await RenderRootComponentAsync(AssignRootComponentId(component), ParameterView.FromDictionary(parameters));
            return component;
        }
    }
    private sealed class TestNavigation : NavigationManager
    {
        public TestNavigation() => Initialize("http://localhost/", "http://localhost/positions");
        protected override void NavigateToCore(string uri, bool forceLoad) { }
    }
    private sealed class NoBrowser : IJSRuntime
    {
        public ValueTask<TValue> InvokeAsync<TValue>(string identifier, object?[]? args) => ValueTask.FromResult(default(TValue)!);
        public ValueTask<TValue> InvokeAsync<TValue>(string identifier, CancellationToken cancellationToken, object?[]? args) => ValueTask.FromResult(default(TValue)!);
    }
}

