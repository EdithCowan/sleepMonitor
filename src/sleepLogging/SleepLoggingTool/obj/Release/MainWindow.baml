   M S B A M L   `   `   `  ����   SleepLoggingTool5    /Microsoft.Samples.Kinect.DepthBasics.MainWindow   \ XPresentationFramework, Version=4.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35R NWindowsBase, Version=4.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35W SPresentationCore, Version=4.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35D 9http://schemas.microsoft.com/winfx/2006/xaml/presentation    R NSystem.Xaml, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089:x,http://schemas.microsoft.com/winfx/2006/xaml     -     � Title$#  Lying Position Logging - V1.0��$	��600��$	��700�� � 	Resources    MediumGreyBrush �� Color  KinectPurpleBrush  KinectBlueBrush �� 
TargetType �� Property �� Value  ScreenshotButton �� 
TargetType �� Orientation ��Name	 �� TextAlignment
 ��Name �� Triggers P� Property P� Value �� 
TargetName��%�  &	        &	      &	 *     '�� ?     &	 t     7  7  �� $ 	#ff6e6e6e���� $ 	#ff52318f���� $ 	#ff00BCF2���� " ��.���  �@J�SnapsToDevicePixels . �� " ��.���  � �� �� " ��.R�� .+��� $ 
Horizontal=�
������ .+��� $ ScreenshotText��#��z��$
	 Left=�$��Center=�8��  $��15(�� $��9,0,0,0q�.+��� $
 ScreenshotNormal��$!��Images\ScreenshotNormal.png��$
��None=�$��Center=��� $
 ScreenshotHover��$ ��Images\ScreenshotHover.png��$
��None=�$��Center=�$y�	Collapsed=� P�  �@J�IsMouseOver . . ��� $ ScreenshotNormal�� � y�$ 	Collapsed=��� $ ScreenshotHover�� � y�$ Visible=��� 8  $ ScreenshotText�� � ��.��� $��	10 0 10 0q� � RowDefinitions �� $
��Auto ��� $��* ��� $
��Auto ��� $
��Auto �.+��� $��0��$��Images\Logo.png��$
��Left=�$
��Fill=�$��32��$��81��$��0 10 0 5q��� $��0��$��	0,31,-1,0q�$��Right=�#��[  $��Segoe UI)�$��18(�Logging Tool�� $��0��$��Images\Status.png��$
��None=�$��Center=�$��0 0 0 5q�(� $��1��$��Center=�.���� $��UniformToFill=����� *ImageSource+�� -   $��	statusBar��$��3��$��Stretch=�$��Bottom=�
�������#��[  .@��� ���� *
StatusText+�� -    ��Name$ colourCheckbox��$��Colour��$
��Left=�$��	19,24,0,0q�$��2��$	��Top=� d� 	IsChecked$
 TrueY��� -   $ depthCheckbox��$��Depth��$
��Left=�$��	90,24,0,0q�$��2��$	��Top=�$
 TrueY��� -   $ infraCheckbox��$��Infrared��$
��Left=�$��
167,24,0,0q�$��2��$	��Top=�$
 TrueY��� -   $ SliderValueDisplay��$
��Left=�$��	357,1,0,0q�$��2��$
��Wrap=�$	��Top=� J� RenderTransformOrigin$ 0.5,0.5&�$��20(�{�.  �� $
�Samp���� $�ling Period���� -    ��Name$ SamplingInterval��$
��Left=�$��
247,24,0,0q�$��2��$	��Top=�$	��349�� � Maximum$ 30Q� � SmallChange$ 1Q� � LargeChange$ 10Q� �� TickPlacement$ TopLeft=� � Value$ 8Q� � ToolTip$ Time between samples.��{�.  �� -   $ SliderLegend��$
��Left=�$��
370,41,0,0q�$��2��$
��Wrap=�$	��Top=�$ 0.5,0.5&�$��2���� $
�Samp���� $�ling Period���� -	    ��Name$ GoButton��$	��Go!��$
��Left=�$��
607,10,0,0q�$��2��$	��Top=�$��55��$��41��