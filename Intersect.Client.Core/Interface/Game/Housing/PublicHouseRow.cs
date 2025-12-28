using Intersect.Client.Core;
using Intersect.Client.Framework.File_Management;
using Intersect.Client.Framework.GenericClasses;
using Intersect.Client.Framework.Gwen.Control;
using Intersect.Client.Framework.Gwen.Control.EventArguments;
using Intersect.Client.Interface.Shared;
using Intersect.Client.Localization;
using Intersect.Client.Networking;
using Intersect.Network.Packets.Server;

namespace Intersect.Client.Interface.Game.Housing;

public partial class PublicHouseRow
{
    private Base _parent;
    private ImagePanel _rowContainer;
    private Label _houseName;
    private Label _ownerName;
    private Label _rating;
    private Label _visits;
    private Label _description;
    private Button _visitButton;
    private Button _rateButton;

    private PublicHouseInfo _houseInfo;

    public PublicHouseRow(Base parent, PublicHouseInfo houseInfo)
    {
        _parent = parent;
        _houseInfo = houseInfo;

        GenerateControls();
        _rowContainer.LoadJsonUi(GameContentManager.UI.InGame, Graphics.Renderer.GetResolutionString());

        UpdateControls();
    }

    private void GenerateControls()
    {
        _rowContainer = new ImagePanel(_parent, "PublicHouseRow");
        _houseName = new Label(_rowContainer, "HouseName");
        _ownerName = new Label(_rowContainer, "OwnerName");
        _rating = new Label(_rowContainer, "Rating");
        _visits = new Label(_rowContainer, "Visits");
        _description = new Label(_rowContainer, "Description");
        _visitButton = new Button(_rowContainer, "VisitButton");
        _rateButton = new Button(_rowContainer, "RateButton");

        _visitButton.Clicked += VisitButton_Clicked;
        _rateButton.Clicked += RateButton_Clicked;
    }

    private void UpdateControls()
    {
        // Update house name
        var houseName = string.IsNullOrWhiteSpace(_houseInfo.HouseName)
            ? $"{_houseInfo.OwnerName}'s House"
            : _houseInfo.HouseName;
        _houseName.SetText(houseName);

        // Update owner name
        _ownerName.SetText($"Owner: {_houseInfo.OwnerName}");

        // Update rating display
        var ratingText = _houseInfo.RatingCount > 0
            ? $"★ {_houseInfo.AverageRating:F1} ({_houseInfo.RatingCount} ratings)"
            : "No ratings yet";
        _rating.SetText(ratingText);

        // Update visit count
        _visits.SetText($"Visits: {_houseInfo.VisitCount}");

        // Update description
        var description = string.IsNullOrWhiteSpace(_houseInfo.HouseDescription)
            ? "No description available"
            : _houseInfo.HouseDescription;
        _description.SetText(description);

        // Update button text
        _visitButton.SetText("Visit");
        _rateButton.SetText("Rate");
    }

    private void VisitButton_Clicked(Base sender, MouseButtonState arguments)
    {
        // TODO: Send packet to visit this house
        // This would need a new event command or packet to enter someone else's house
        // For now, show a message
        var inputBox = new InputBox(
            title: "Visit House",
            prompt: $"Visit {_houseInfo.OwnerName}'s house?\n\n" +
                   $"Note: This feature requires the game designer to create an NPC or event that uses the EnterHouse command with the owner's player variable.",
            inputType: InputType.OkayOnly,
            onSubmit: null
        );
    }

    private void RateButton_Clicked(Base sender, MouseButtonState arguments)
    {
        // TODO: Send packet to rate this house (1-5 stars)
        // This would use the RateHouse event command
        var inputBox = new InputBox(
            title: "Rate House",
            prompt: $"Rate {_houseInfo.OwnerName}'s house?\n\n" +
                   $"Note: This feature requires the game designer to create an NPC or event that uses the RateHouse command.",
            inputType: InputType.OkayOnly,
            onSubmit: null
        );
    }

    public Rectangle Bounds => _rowContainer.Bounds;

    public void SetPosition(float x, float y) => _rowContainer.SetPosition(x, y);

    public void Dispose()
    {
        _parent.RemoveChild(_rowContainer, true);
    }
}
