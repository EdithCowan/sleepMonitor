   M S B A M L   `   `   `  ����  SleepLoggingTool5    /Microsoft.Samples.Kinect.DepthBasics.MainWindow   5   	   \ XPresentationFramework, Version=4.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35R NWindowsBase, Version=4.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35W SPresentationCore, Version=4.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35D 9http://schemas.microsoft.com/winfx/2006/xaml/presentation    5   	   R NSystem.Xaml, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089:x,http://schemas.microsoft.com/winfx/2006/xaml     5   	   -   5          � Title$  Sleep Logging��5   	   $	��600��5   	   $	��700��6    � 	Resources 5         MediumGreyBrush �� Color  KinectPurpleBrush  KinectBlueBrush �� 
TargetType �� Property �� Value  ScreenshotButton �� 
TargetType �� Orientation ��Name	 �� TextAlignment
 ��Name �� Triggers P� Property P� Value �� 
TargetName��%a  &	        &	 #     &	 F     '�� i     &	 �     7  7  �� 5   
   $ 	#ff6e6e6e��62   �� 5	   
   $ 	#ff52318f��64   �� 5
   
   $ 	#ff00BCF2��62   �� 5   
   " ��6   .�5      ��  �@J�SnapsToDevicePixels . 64   5      �� 5   
   " ��6   .�5      ��  � �� 5      �� 5      " ��6&   .R�5      � .+�5      �� $ 
Horizontal=�6)   
������ .+�5   "   �� $ ScreenshotText��6,   #��z��6D   $
	 Left=�6e   $��Center=�6{   8��  $��15(�6�   � 5   "   $��9,0,0,0q�6'   .+�5   &   �� $
 ScreenshotNormal��6,   $!��Images\ScreenshotNormal.png��6F   $
��None=�6k   $��Center=�6z   �� 5   &   $
 ScreenshotHover��6,   $ ��Images\ScreenshotHover.png��6E   $
��None=�6i   $��Center=�6x   $y�	Collapsed=�6�   5   #   5      5       5      P� 5       �@J�IsMouseOver . . �5   "   �� $ ScreenshotNormal��6Q    � y�$ 	Collapsed=�6?   6Q   �� 5   "   $ ScreenshotHover��6O    � y�$ Visible=�6?   6O   �� 5   "   8  $ ScreenshotText��6h    � ��5       5"      5$      5%      5&      .��5'      � $��	10 0 10 0q�6    � RowDefinitions 5(   
   �� 5)      $
��Auto �6   �� 5*      $��* �6   �� 5+      $
��Auto �6   �� 5,      $
��Auto �6   .+�5/   
   �� $��0��6   $��Images\Logo.png��6   $
��Left=�66   $
��Fill=�6Q   $��32��6`   $��81��6l   $��0 10 0 5q�6w   �� 50   
   $��0��6   $��	0,31,-1,0q�6!   $��Right=�64   #��[  6P   $��Segoe UI)�6~   $��18(�6�   Logging Tool6�   �� 51   
   $��0��6   $��Images\Status.png��6   $
��None=�68   $��Center=�6G   $��0 0 0 5q�6d   (� 53   
   $��1��6   $��Center=�6   .��54      �� $��UniformToFill=�63   ��6   �� *ImageSource+63   55      �� 57   
   -   5        $��	statusBar��57   ?   $��3��6   $��Stretch=�6!   $��Bottom=�6P   
�������#��[  6~   .@�58      �� ��6   �� *
StatusText+59      �� 5:   
   -   5         ��Name$ colourCheckbox��5:      $��Colour��6+   $
��Left=�6<   $��	19,24,0,0q�6W   $��2��6j   $	��Top=�6w    d� 	IsChecked$
 TrueY�6�   �� 5;   
   -   5        $ depthCheckbox��5;      $��Depth��6*   $
��Left=�6:   $��	90,24,0,0q�6U   $��2��6h   $	��Top=�6u   $
 TrueY�6�   �� 5<   
   -   5        $ infraCheckbox��5<      $��Infrared��6*   $
��Left=�6=   $��
167,24,0,0q�6X   $��2��6l   $	��Top=�6y   $
 TrueY�6�   �� 5=   
   -   5        $ SliderValueDisplay��5=      $
��Left=�60   $��	357,1,0,0q�6K   $��2��6^   $
��Wrap=�6k   $	��Top=�6    J� RenderTransformOrigin$ 0.5,0.5&�6�   $��20(�6�   {�.  �� 6�   $
�Samp��6�   �� 6�   $�ling Period��6�   6  �� 5>   
   -   5         ��Name$ SamplingInterval��5>      $
��Left=�6+   $��
247,24,0,0q�6F   $��2��6Z   $	��Top=�6g   $	��349��6    � Maximum$ 30Q�6�    � SmallChange$ 1Q�6�    � LargeChange$ 10Q�6�    �� TickPlacement$ TopLeft=�6�    � Value$ 8Q�6�    � ToolTip$ Time between samples.��6�   {�.  6  �� 5?   
   -   5        $ SliderLegend��5?      $
��Left=�6*   $��
370,41,0,0q�6E   $��2��6Y   $
��Wrap=�6f   $	��Top=�6z   $ 0.5,0.5&�6�   $��2��6�   �� 6�   $
�Samp��6�   �� 6�   $�ling Period��6�   6�   �� 5@   
   -	   5         ��Name$ GoButton��5@      $	��Go!��6#   $
��Left=�61   $��
607,10,0,0q�6L   $��2��6`   $	��Top=�6m   $��55��6�   $��41��6�   6�   5A      5B      