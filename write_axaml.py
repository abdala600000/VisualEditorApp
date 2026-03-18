content = '''<UserControl xmlns="https://github.com/avaloniaui"
             xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
             xmlns:d="http://schemas.microsoft.com/expression/blend/2008"
             xmlns:mc="http://schemas.openxmlformats.org/markup-compatibility/2006"
             mc:Ignorable="d" d:DesignWidth="800" d:DesignHeight="450"
             xmlns:paz="clr-namespace:Avalonia.Controls.PanAndZoom;assembly=PanAndZoom"
             x:Class="VisualEditor.Designer.DesignerSurfaceView">

    <UserControl.Resources>
        <VisualBrush x:Key="GridBackgroundBrush"
                     TileMode="Tile"
                     SourceRect="0,0,20,20"
                     DestinationRect="0,0,20,20">
            <VisualBrush.Visual>
                <Canvas Width="20" Height="20">
                    <Path Data="M 20 0 L 0 0 0 20" Stroke="#CCCCCC" StrokeThickness="0.5" />
                </Canvas>
            </VisualBrush.Visual>
        </VisualBrush>
    </UserControl.Resources>

    <UserControl.Styles>
        <Style Selector="Thumb.ResizerThumb">
            <Setter Property="Width" Value="8"/>
            <Setter Property="Height" Value="8"/>
            <Setter Property="Template">
                <ControlTemplate>
                    <Border Background="White" BorderBrush="DodgerBlue" BorderThickness="1.5"/>
                </ControlTemplate>
            </Setter>
        </Style>
    </UserControl.Styles>

    <Grid RowDefinitions="25,*" ColumnDefinitions="25,*">

        <Border Grid.Row="0" Grid.Column="0" Background="#E8E8E8" BorderBrush="#D0D0D0" BorderThickness="0,0,1,1"/>

        <Canvas Grid.Row="0" Grid.Column="1" Background="#F0F0F0" Name="TopRulerContainer" ClipToBounds="True">
            <Canvas Name="TopRuler" />
        </Canvas>

        <Canvas Grid.Row="1" Grid.Column="0" Background="#F0F0F0" Name="LeftRulerContainer" ClipToBounds="True">
            <Canvas Name="LeftRuler" />
        </Canvas>

        <ScrollViewer Grid.Row="1" Grid.Column="1"
                      HorizontalScrollBarVisibility="Auto"
                      VerticalScrollBarVisibility="Auto">
            <Grid Name="DesignerContainer">

                <paz:ZoomBorder x:Name="MyZoomBorder"
                                Stretch="None"
                                ZoomSpeed="1.2"
                                PanButton="Middle"
                                EnableConstrains="False"
                                ClipToBounds="True"
                                Focusable="True"
                                VerticalAlignment="Stretch"
                                HorizontalAlignment="Stretch"
                                Background="{StaticResource GridBackgroundBrush}">

                    <Border Padding="80">
                        <Border x:Name="DesignSurfaceWrapper"
                                Background="White"
                                HorizontalAlignment="Left"
                                VerticalAlignment="Top"
                                BoxShadow="0 2 8 0 #25000000, 0 8 24 0 #15000000">
                            <Panel>
                                <ContentControl x:Name="DesignSurface"
                                                HorizontalAlignment="Stretch"
                                                VerticalAlignment="Stretch" />
                                <Border x:Name="DropLayer"
                                        Background="Transparent"
                                        HorizontalAlignment="Stretch"
                                        VerticalAlignment="Stretch"
                                        IsHitTestVisible="True" />
                            </Panel>
                        </Border>
                    </Border>

                </paz:ZoomBorder>

                <Canvas Name="AdornerCanvas" IsHitTestVisible="False"
                    PointerMoved="RotateHandle_PointerMoved"
                    PointerReleased="RotateHandle_PointerReleased">
                    <Rectangle Name="SelectionBox"
                               Fill="#202196F3"
                               Stroke="DodgerBlue"
                               StrokeThickness="1"
                               StrokeDashArray="4,2"
                               IsVisible="False" />
                    <Border Name="SelectionAdorner" BorderBrush="DodgerBlue" BorderThickness="1.5" IsVisible="False">
                        <Grid>
                            <Ellipse Name="RotateHandle" Width="12" Height="12"
                                   Fill="DodgerBlue" Stroke="White" StrokeThickness="1.5"
                                   HorizontalAlignment="Center" VerticalAlignment="Top"
                                   Margin="0,-28,0,0"
                                   Cursor="Hand" ToolTip.Tip="Rotate"
                                   PointerPressed="RotateHandle_PointerPressed" />
                            <Line StartPoint="0,-4" EndPoint="0,-22"
                                  HorizontalAlignment="Center" VerticalAlignment="Top"
                                  Stroke="DodgerBlue" StrokeThickness="1"
                                  IsHitTestVisible="False"/>
                            <Thumb Name="TopLeft"     Classes="ResizerThumb" HorizontalAlignment="Left"   VerticalAlignment="Top"    Margin="-4,-4,0,0"  DragDelta="Resize_DragDelta" DragCompleted="Resize_DragCompleted" />
                            <Thumb Name="TopRight"    Classes="ResizerThumb" HorizontalAlignment="Right"  VerticalAlignment="Top"    Margin="0,-4,-4,0"  DragDelta="Resize_DragDelta" DragCompleted="Resize_DragCompleted" />
                            <Thumb Name="BottomLeft"  Classes="ResizerThumb" HorizontalAlignment="Left"   VerticalAlignment="Bottom" Margin="-4,0,0,-4"  DragDelta="Resize_DragDelta" DragCompleted="Resize_DragCompleted" />
                            <Thumb Name="BottomRight" Classes="ResizerThumb" HorizontalAlignment="Right"  VerticalAlignment="Bottom" Margin="0,0,-4,-4"  DragDelta="Resize_DragDelta" DragCompleted="Resize_DragCompleted" />
                        </Grid>
                    </Border>
                </Canvas>

            </Grid>
        </ScrollViewer>
    </Grid>
</UserControl>'''

with open('VisualEditor.Designer/DesignerSurfaceView.axaml', 'w', encoding='utf-8') as f:
    f.write(content)
print('Done')
