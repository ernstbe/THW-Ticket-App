using Microsoft.Maui.Controls;

namespace THWTicketApp.Controls;

public class SkeletonView : ContentView
{
    public static readonly BindableProperty SkeletonTypeProperty =
        BindableProperty.Create(nameof(SkeletonType), typeof(string), typeof(SkeletonView), "list",
            propertyChanged: OnSkeletonTypeChanged);

    public string SkeletonType
    {
        get => (string)GetValue(SkeletonTypeProperty);
        set => SetValue(SkeletonTypeProperty, value);
    }

    public SkeletonView()
    {
        BuildContent();
    }

    private static void OnSkeletonTypeChanged(BindableObject bindable, object oldValue, object newValue)
    {
        ((SkeletonView)bindable).BuildContent();
    }

    private void BuildContent()
    {
        Content = SkeletonType?.ToLowerInvariant() switch
        {
            "dashboard" => BuildDashboardSkeleton(),
            "kanban" => BuildKanbanSkeleton(),
            _ => BuildListSkeleton()
        };

        StartAnimation();
    }

    private View BuildListSkeleton()
    {
        var stack = new VerticalStackLayout { Spacing = 10, Padding = new Thickness(10) };
        for (int i = 0; i < 5; i++)
        {
            stack.Children.Add(BuildTicketCardSkeleton());
        }
        return stack;
    }

    private View BuildDashboardSkeleton()
    {
        var stack = new VerticalStackLayout { Spacing = 15, Padding = new Thickness(15) };

        // Stats grid skeleton
        var statsGrid = new Grid
        {
            ColumnDefinitions = { new ColumnDefinition(), new ColumnDefinition(), new ColumnDefinition() },
            RowDefinitions = { new RowDefinition(GridLength.Auto), new RowDefinition(GridLength.Auto) },
            ColumnSpacing = 10,
            RowSpacing = 10
        };

        for (int row = 0; row < 2; row++)
            for (int col = 0; col < 3; col++)
            {
                var card = BuildSkeletonBox(0, 80);
                card.SetValue(Grid.RowProperty, row);
                card.SetValue(Grid.ColumnProperty, col);
                statsGrid.Children.Add(card);
            }
        stack.Children.Add(statsGrid);

        // Recent tickets skeleton
        stack.Children.Add(BuildSkeletonBox(200, 0));
        for (int i = 0; i < 3; i++)
            stack.Children.Add(BuildTicketCardSkeleton());

        return stack;
    }

    private View BuildKanbanSkeleton()
    {
        var stack = new HorizontalStackLayout { Spacing = 10, Padding = new Thickness(10) };
        for (int i = 0; i < 3; i++)
        {
            var column = new VerticalStackLayout { Spacing = 8, WidthRequest = 280 };
            column.Children.Add(BuildSkeletonBox(30, 0));
            for (int j = 0; j < 4; j++)
                column.Children.Add(BuildSkeletonBox(70, 0));

            var border = new Border
            {
                Content = column,
                StrokeShape = new Microsoft.Maui.Controls.Shapes.RoundRectangle { CornerRadius = 10 },
                Padding = new Thickness(10),
                Stroke = Colors.Transparent
            };
            border.SetAppThemeColor(Border.BackgroundColorProperty,
                Color.FromArgb("#FFFFFF"), Color.FromArgb("#1E1E1E"));
            stack.Children.Add(border);
        }
        return new ScrollView { Orientation = ScrollOrientation.Horizontal, Content = stack };
    }

    private static View BuildTicketCardSkeleton()
    {
        var grid = new Grid
        {
            ColumnDefinitions = { new ColumnDefinition(new GridLength(5)), new ColumnDefinition() },
            Padding = new Thickness(0),
            HeightRequest = 80
        };

        var bar = new BoxView { CornerRadius = 2 };
        bar.SetAppThemeColor(BoxView.BackgroundColorProperty,
            Color.FromArgb("#E0E0E0"), Color.FromArgb("#333333"));
        bar.SetValue(Grid.ColumnProperty, 0);
        grid.Children.Add(bar);

        var content = new VerticalStackLayout { Spacing = 8, Padding = new Thickness(12, 8) };
        content.Children.Add(BuildSkeletonLine(0.7));
        content.Children.Add(BuildSkeletonLine(0.4));
        content.Children.Add(BuildSkeletonLine(0.5));
        content.SetValue(Grid.ColumnProperty, 1);
        grid.Children.Add(content);

        var border = new Border
        {
            Content = grid,
            StrokeShape = new Microsoft.Maui.Controls.Shapes.RoundRectangle { CornerRadius = 10 },
            StrokeThickness = 1,
            Margin = new Thickness(10, 5)
        };
        border.SetAppThemeColor(Border.StrokeProperty,
            Color.FromArgb("#E0E0E0"), Color.FromArgb("#333333"));
        border.SetAppThemeColor(Border.BackgroundColorProperty,
            Color.FromArgb("#FFFFFF"), Color.FromArgb("#1E1E1E"));

        return border;
    }

    private static BoxView BuildSkeletonLine(double widthFraction)
    {
        var box = new BoxView
        {
            HeightRequest = 14,
            CornerRadius = 4,
            HorizontalOptions = LayoutOptions.Start
        };
        box.SetAppThemeColor(BoxView.BackgroundColorProperty,
            Color.FromArgb("#E8E8E8"), Color.FromArgb("#2C2C2C"));

        // Use percentage-based width via binding
        box.WidthRequest = widthFraction * 250;
        return box;
    }

    private static View BuildSkeletonBox(double height, double minHeight)
    {
        var box = new BoxView
        {
            CornerRadius = 8,
            HeightRequest = height > 0 ? height : 80,
            MinimumHeightRequest = minHeight > 0 ? minHeight : 0
        };
        box.SetAppThemeColor(BoxView.BackgroundColorProperty,
            Color.FromArgb("#E8E8E8"), Color.FromArgb("#2C2C2C"));
        return box;
    }

    private void StartAnimation()
    {
        // Pulse animation on all skeleton elements
        this.Dispatcher?.Dispatch(async () =>
        {
            while (IsVisible && Content != null)
            {
                await this.FadeToAsync(0.4, 800, Easing.SinInOut);
                await this.FadeToAsync(1.0, 800, Easing.SinInOut);
            }
        });
    }
}
