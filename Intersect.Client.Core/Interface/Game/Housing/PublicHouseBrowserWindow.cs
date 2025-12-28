using Intersect.Client.Core;
using Intersect.Client.Framework.File_Management;
using Intersect.Client.Framework.Gwen.Control;
using Intersect.Client.Framework.Gwen.Control.EventArguments;
using Intersect.Client.General;
using Intersect.Client.Localization;
using Intersect.Network.Packets.Server;

namespace Intersect.Client.Interface.Game.Housing;

public partial class PublicHouseBrowserWindow
{
    private Base _parent;
    private WindowControl _window;
    private Button _searchButton;
    private Button _refreshButton;
    private ScrollControl _houseContainer;
    private ImagePanel _houseListAnchor;
    private Label _resultCountLabel;
    private TextBox _searchTextBox;

    private List<PublicHouseRow> _rows;

    public PublicHouseBrowserWindow(Canvas gameCanvas)
    {
        _parent = gameCanvas;

        GenerateControls();
        _window.LoadJsonUi(GameContentManager.UI.InGame, Graphics.Renderer.GetResolutionString());

        UpdateList(null);
    }

    private void GenerateControls()
    {
        _window = new WindowControl(_parent, "Public House Tours", false, "PublicHouseBrowserWindow");
        _searchTextBox = new TextBox(_window, "SearchTextBox");
        _searchButton = new Button(_window, "SearchButton");
        _refreshButton = new Button(_window, "RefreshButton");
        _houseContainer = new ScrollControl(_window, "HouseContainer");
        _houseListAnchor = new ImagePanel(_houseContainer, "HouseListAnchor");
        _resultCountLabel = new Label(_window, "ResultCountLabel");

        _window.DisableResizing();
        _houseContainer.EnableScroll(false, true);

        _searchButton.SetText("Search");
        _refreshButton.SetText("Refresh");
        _searchTextBox.SetPlaceholderText("Search by owner name or house name...");

        _searchButton.Clicked += SearchButton_Clicked;
        _refreshButton.Clicked += RefreshButton_Clicked;
    }

    public bool IsVisible => !_window.IsHidden;

    public void Show() => _window.Show();

    public void Hide() => _window.Hide();

    public void Update()
    {
        if (!IsVisible)
        {
            ClearList();
        }
    }

    private void ClearList()
    {
        if (_rows != null && _rows.Count > 0)
        {
            foreach (var control in _rows)
            {
                control.Dispose();
            }
            _rows.Clear();
        }
        else if (_rows == null)
        {
            _rows = new List<PublicHouseRow>();
        }
    }

    public void UpdateList(PublicHouseListPacket? packet)
    {
        ClearList();

        if (packet == null || packet.Houses == null || packet.Houses.Length == 0)
        {
            _resultCountLabel.SetText("No public houses available for touring.");
            return;
        }

        _resultCountLabel.SetText($"Showing {packet.Houses.Length} of {packet.TotalCount} public houses");

        var count = 0;
        foreach (var house in packet.Houses)
        {
            var control = new PublicHouseRow(_houseContainer, house);
            control.SetPosition(
                _houseListAnchor.Bounds.X,
                _houseListAnchor.Bounds.Y + (count * control.Bounds.Height)
            );

            _rows.Add(control);
            count++;
        }
    }

    private void SearchButton_Clicked(Base sender, MouseButtonState arguments)
    {
        // TODO: Send packet to server to search for public houses
        // For now, just show a message
        _resultCountLabel.SetText("Search functionality requires server-side implementation.");
    }

    private void RefreshButton_Clicked(Base sender, MouseButtonState arguments)
    {
        // TODO: Send packet to server to refresh the public house list
        // For now, just show a message
        _resultCountLabel.SetText("Refresh functionality requires server-side implementation.");
    }
}
