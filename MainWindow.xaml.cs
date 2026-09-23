using System.Windows;

namespace SpectrumComparison
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        private void BtnSpectrum_Click(object sender, RoutedEventArgs e)
        {
            contentControl.ContentTemplate = (DataTemplate)FindResource("SpectrumViewTemplate");
            contentControl.Content = new SpectrumView();
        }

        private void BtnArtificialWave_Click(object sender, RoutedEventArgs e)
        {
            contentControl.ContentTemplate = (DataTemplate)FindResource("ArtificialWaveViewTemplate");
            contentControl.Content = new ArtificialWaveView();
        }

        private void BtnWind_Click(object sender, RoutedEventArgs e)
        {
            contentControl.ContentTemplate = (DataTemplate)FindResource("WindConversionViewTemplate");
            contentControl.Content = new WindConversionView();
        }

        private void BtnGust_Click(object sender, RoutedEventArgs e)
        {
            contentControl.ContentTemplate = (DataTemplate)FindResource("GustEffectFactorViewTemplate");
            contentControl.Content = new GustEffectFactorView();
        }

        private void BtnWindSim_Click(object sender, RoutedEventArgs e)
        {
            contentControl.ContentTemplate = (DataTemplate)FindResource("WindSimulationViewTemplate");
            contentControl.Content = new WindSimulationView();
        }

        private void BtnBeam_Click(object sender, RoutedEventArgs e)
        {
            contentControl.ContentTemplate = (DataTemplate)FindResource("BeamDesignViewTemplate");
            contentControl.Content = new BeamDesignView();
        }

        private void BtnColumn_Click(object sender, RoutedEventArgs e)
        {
            contentControl.ContentTemplate = (DataTemplate)FindResource("ColumnDesignViewTemplate");
            contentControl.Content = new ColumnDesignView();
        }

        private void BtnCircularColumn_Click(object sender, RoutedEventArgs e)
        {
            contentControl.ContentTemplate = (DataTemplate)FindResource("CircularColumnDesignViewTemplate");
            contentControl.Content = new CircularColumnDesignView();
        }

        private void BtnPunchingShear_Click(object sender, RoutedEventArgs e)
        {
            contentControl.ContentTemplate = (DataTemplate)FindResource("PunchingShearViewTemplate");
            contentControl.Content = new PunchingShearView();
        }

        private void BtnDevelopmentLength_Click(object sender, RoutedEventArgs e)
        {
            contentControl.ContentTemplate = (DataTemplate)FindResource("DevelopmentLengthViewTemplate");
            contentControl.Content = new DevelopmentLengthView();
        }

        private void BtnSteelBeam_Click(object sender, RoutedEventArgs e)
        {
            contentControl.ContentTemplate = (DataTemplate)FindResource("SteelBeamViewTemplate");
            contentControl.Content = new SteelBeamView();
        }

        private void BtnSteelTension_Click(object sender, RoutedEventArgs e)
        {
            contentControl.ContentTemplate = (DataTemplate)FindResource("SteelTensionViewTemplate");
            contentControl.Content = new SteelTensionView();
        }

        private void BtnSteelCompression_Click(object sender, RoutedEventArgs e)
        {
            contentControl.ContentTemplate = (DataTemplate)FindResource("SteelCompressionViewTemplate");
            contentControl.Content = new SteelCompressionView();
        }

        private void BtnRbs_Click(object sender, RoutedEventArgs e)
        {
            contentControl.ContentTemplate = (DataTemplate)FindResource("RbsViewTemplate");
            contentControl.Content = new RbsView();
        }

        private void BtnDuctilityClassification_Click(object sender, RoutedEventArgs e)
        {
            contentControl.ContentTemplate = (DataTemplate)FindResource("DuctilityClassificationViewTemplate");
            contentControl.Content = new DuctilityClassificationView();
        }

        private void BtnStrongColumnWeakBeam_Click(object sender, RoutedEventArgs e)
        {
            contentControl.ContentTemplate = (DataTemplate)FindResource("StrongColumnWeakBeamViewTemplate");
            contentControl.Content = new StrongColumnWeakBeamView();
        }

        private void BtnBeamStabilityBracing_Click(object sender, RoutedEventArgs e)
        {
            contentControl.ContentTemplate = (DataTemplate)FindResource("BeamStabilityBracingViewTemplate");
            contentControl.Content = new BeamStabilityBracingView();
        }

        private void BtnPanelZone_Click(object sender, RoutedEventArgs e)
        {
            contentControl.ContentTemplate = (DataTemplate)FindResource("PanelZoneViewTemplate");
            contentControl.Content = new PanelZoneView();
        }

        private void BtnBfp_Click(object sender, RoutedEventArgs e)
        {
            contentControl.ContentTemplate = (DataTemplate)FindResource("BfpViewTemplate");
            contentControl.Content = new BfpView();
        }

        private void BtnEndplate_Click(object sender, RoutedEventArgs e)
        {
            contentControl.ContentTemplate = (DataTemplate)FindResource("EndplateViewTemplate");
            contentControl.Content = new EndplateView();
        }

        private void BtnWufw_Click(object sender, RoutedEventArgs e)
        {
            contentControl.ContentTemplate = (DataTemplate)FindResource("WufwViewTemplate");
            contentControl.Content = new WufwView();
        }

        private void BtnKbb_Click(object sender, RoutedEventArgs e)
        {
            contentControl.ContentTemplate = (DataTemplate)FindResource("KbbViewTemplate");
            contentControl.Content = new KbbView();
        }

        private void BtnConxl_Click(object sender, RoutedEventArgs e)
        {
            contentControl.ContentTemplate = (DataTemplate)FindResource("ConxlViewTemplate");
            contentControl.Content = new ConxlView();
        }

        private void BtnSideplate_Click(object sender, RoutedEventArgs e)
        {
            contentControl.ContentTemplate = (DataTemplate)FindResource("SideplateViewTemplate");
            contentControl.Content = new SideplateView();
        }

        private void BtnSst_Click(object sender, RoutedEventArgs e)
        {
            contentControl.ContentTemplate = (DataTemplate)FindResource("SstViewTemplate");
            contentControl.Content = new SstView();
        }

        private void BtnDoubletee_Click(object sender, RoutedEventArgs e)
        {
            contentControl.ContentTemplate = (DataTemplate)FindResource("DoubleteeViewTemplate");
            contentControl.Content = new DoubleteeView();
        }

        private void BtnSlottedweb_Click(object sender, RoutedEventArgs e)
        {
            contentControl.ContentTemplate = (DataTemplate)FindResource("SlottedwebViewTemplate");
            contentControl.Content = new SlottedwebView();
        }

        private void BtnWeChat_Click(object sender, RoutedEventArgs e)
        {
            contentControl.ContentTemplate = (DataTemplate)FindResource("WeChatViewTemplate");
            contentControl.Content = new WeChatView();
        }
    }
}
