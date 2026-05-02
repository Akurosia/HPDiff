using System;
using System.Numerics;

using CheapLoc;

using Dalamud.Bindings.ImGui;

namespace HPDiff;

public sealed class PluginUI : IDisposable
{
	public PluginUI( Plugin plugin, Configuration configuration )
	{
		mPlugin = plugin;
		mConfiguration = configuration;
	}

	public void Initialize()
	{
	}

	public void Dispose()
	{
	}

	public void Draw()
	{
		//	Draw the sub-windows.
		DrawSettingsWindow();
		DrawGaugeWindow();
	}

	private void DrawSettingsWindow()
	{
		if( !mSettingsWindowVisible ) return;

		if( ImGui.Begin( Loc.Localize( "Window Title: Config", "HP Difference Gauge Settings" ) + "###HPDiff Settings",
			ref mSettingsWindowVisible,
			ImGuiWindowFlags.AlwaysAutoResize | ImGuiWindowFlags.NoCollapse ) )
		{
			ImGui.Checkbox( "Lock Gauge Position/Size", ref mConfiguration.mLockGaugeWindow );
			ImGui.Checkbox( "Gauge Clickthrough", ref mConfiguration.mGaugeClickthrough);
			ImGui.ColorEdit4( "Indicator Color", ref mConfiguration.mIndicatorColor, ImGuiColorEditFlags.NoInputs );
			ImGui.ColorEdit4( "Indicator Color (Above Threshold)", ref mConfiguration.mIndicatorWarningColor, ImGuiColorEditFlags.NoInputs );
			ImGui.ColorEdit4( "Threshold Color", ref mConfiguration.mThresholdIndicatorColor, ImGuiColorEditFlags.NoInputs );

			ImGui.Spacing();
			ImGui.Spacing();
			ImGui.Spacing();
			ImGui.Spacing();
			ImGui.Spacing();

			ImGui.Separator();

			ImGui.Spacing();
			ImGui.Spacing();
			ImGui.Spacing();
			ImGui.Spacing();
			ImGui.Spacing();

			if( ImGui.Button( "Add Gauge Config" ) )
			{
				mConfiguration.DiffGaugeConfigs.Add( new() );
			}

			int gaugeIndexToDelete = -1;
			for( int i = 0; i < mConfiguration.DiffGaugeConfigs.Count; ++i )
			{
				var config = mConfiguration.DiffGaugeConfigs[i];
				ImGui.PushID( $"GaugeConfig{i}." );

				if( ImGui.CollapsingHeader( $"{config.mName} {(config.mEnabled ? "" : "(Disabled)")}###Header" ) )
				{
					ImGui.Checkbox( "Enabled", ref config.mEnabled );
					ImGui.InputText( "Gauge Name", ref config.mName, 128 );
					ImGui.InputInt( "TerritoryType", ref config.mTerritoryType );
					ImGui.InputText( "Enemy 1 Name", ref config.mEnemy1Name, 64 );
					ImGui.InputText( "Enemy 2 Name", ref config.mEnemy2Name, 64 );
					ImGui.ColorEdit4( "Enemy 1 Color", ref config.mLeftColor, ImGuiColorEditFlags.NoInputs );
					ImGui.ColorEdit4( "Enemy 2 Color", ref config.mRightColor, ImGuiColorEditFlags.NoInputs );
					ImGui.SliderFloat( "Max HP Difference To Show", ref config.mGaugeHalfRange_Pct, 1f, 50f, "%f", ImGuiSliderFlags.AlwaysClamp );
					ImGui.SliderFloat( "Desired HP Threshold", ref config.mDiffThreshold_Pct, 1f, 50f, "%f", ImGuiSliderFlags.AlwaysClamp );

					if( ImGui.Button( Loc.Localize( "Button: Delete Distance Widget", "Delete Widget" ) + $"###Delete Widget Button {i}." ) )
					{
						mGaugeIndexWantToDelete = i;
					}
					if( mGaugeIndexWantToDelete == i )
					{
						ImGui.PushStyleColor( ImGuiCol.Text, 0xee4444ff );
						ImGui.Text( Loc.Localize( "Settings Window Text: Confirm Delete Label", "Confirm delete: " ) );
						ImGui.SameLine();
						if( ImGui.Button( Loc.Localize( "Button: Yes", "Yes" ) + $"###Delete Widget Yes Button {i}" ) )
						{
							gaugeIndexToDelete = mGaugeIndexWantToDelete;
						}
						ImGui.PopStyleColor();
						ImGui.SameLine();
						if( ImGui.Button( Loc.Localize( "Button: No", "No" ) + $"###Delete Widget No Button {i}" ) )
						{
							mGaugeIndexWantToDelete = -1;
						}
					}
				}

				ImGui.PopID();
			}
			if( gaugeIndexToDelete > -1 && gaugeIndexToDelete < mConfiguration.DiffGaugeConfigs.Count )
			{
				mConfiguration.DiffGaugeConfigs.RemoveAt( gaugeIndexToDelete );
				mGaugeIndexWantToDelete = -1;
			}

			ImGui.Spacing();
			ImGui.Spacing();
			ImGui.Spacing();
			ImGui.Spacing();
			ImGui.Spacing();

			if( ImGui.Button( Loc.Localize( "Button: Save", "Save" ) + "###Save Button" ) )
			{
				mConfiguration.Save();
			}
			ImGui.SameLine();
			if( ImGui.Button( Loc.Localize( "Button: Save and Close", "Save and Close" ) + "###Save and Close Button" ) )
			{
				mConfiguration.Save();
				mSettingsWindowVisible = false;
			}
		}

		ImGui.End();
	}

	private void DrawGaugeWindow()
	{
		var gaugeData = mPlugin.GaugeDrawData;
		if( !gaugeData.mShouldDraw && mSettingsWindowVisible ) gaugeData = DiffGaugeDrawData.PreviewGaugeData;
		if( !gaugeData.mShouldDraw ) return;

		ImGuiWindowFlags windowFlags =  ImGuiWindowFlags.NoTitleBar |															
										ImGuiWindowFlags.NoFocusOnAppearing |
										ImGuiWindowFlags.NoNav;

		if( mConfiguration.mLockGaugeWindow ) windowFlags |= ImGuiWindowFlags.NoMove | ImGuiWindowFlags.NoResize;
		if( mConfiguration.mGaugeClickthrough ) windowFlags |= ImGuiWindowFlags.NoMouseInputs;

		ImGui.SetNextWindowSize( new Vector2( 600, 64 ) * ImGui.GetIO().FontGlobalScale, ImGuiCond.FirstUseEver );
		if( ImGui.Begin( "###HPDiff Gauge Window", windowFlags ) )
		{
			Vector2 gaugeBase = ImGui.GetWindowPos() + ImGui.GetWindowContentRegionMin();
			Vector2 gaugeSize = ImGui.GetContentRegionAvail();

			float pixelsPerPercent = gaugeSize.X / ( Math.Max( 0.1f, gaugeData.mGaugeHalfRange_Pct ) * 2f );

			float triangleWidth = gaugeSize.X / 20f;
			float triangleHeight = gaugeSize.Y / 3f;
			float triangleCenterX = pixelsPerPercent * ( gaugeData.mGaugeHalfRange_Pct + Math.Min( Math.Max( -gaugeData.mGaugeHalfRange_Pct, gaugeData.HPDiff_Pct ), gaugeData.mGaugeHalfRange_Pct ) );
			Vector4 triangleColor = Math.Abs( gaugeData.HPDiff_Pct ) < gaugeData.mDiffThreshold_Pct ? mConfiguration.mIndicatorColor : mConfiguration.mIndicatorWarningColor;

			Vector2 leftBarBase = new( gaugeBase.X, gaugeBase.Y + gaugeSize.Y / 3f );
			Vector2 leftBarSize = new( gaugeSize.X / 2f, gaugeSize.Y / 3f );

			Vector2 rightBarBase = new( gaugeBase.X + gaugeSize.X / 2f, gaugeBase.Y + gaugeSize.Y / 3f );
			Vector2 rightBarSize = new( gaugeSize.X / 2f, gaugeSize.Y / 3f );

			ImGui.GetWindowDrawList().AddQuadFilled( leftBarBase, new( leftBarBase.X + leftBarSize.X, leftBarBase.Y ), leftBarBase + leftBarSize, new( leftBarBase.X, leftBarBase.Y + leftBarSize.Y ), ImGuiUtils.ColorVecToUInt( gaugeData.mLeftColor ) );
			ImGui.GetWindowDrawList().AddQuadFilled( rightBarBase, new( rightBarBase.X + rightBarSize.X, rightBarBase.Y ), rightBarBase + rightBarSize, new( rightBarBase.X, rightBarBase.Y + rightBarSize.Y ), ImGuiUtils.ColorVecToUInt( gaugeData.mRightColor ) );

			float enemy2TextWidth = ImGui.CalcTextSize( gaugeData.mEnemy2Name ).X;
			ImGui.GetWindowDrawList().AddText( gaugeBase, ImGuiUtils.ColorVecToUInt( Vector4.One ), gaugeData.mEnemy1Name );
			ImGui.GetWindowDrawList().AddText( gaugeBase + new Vector2( gaugeSize.X - enemy2TextWidth, 0 ), ImGuiUtils.ColorVecToUInt( Vector4.One ), gaugeData.mEnemy2Name );

			float rightPctTextWidth = ImGui.CalcTextSize( $"{gaugeData.mGaugeHalfRange_Pct:F1}" ).X;
			ImGui.GetWindowDrawList().AddText( gaugeBase + new Vector2( 0, gaugeSize.Y / 3f * 2f + 1f ), ImGuiUtils.ColorVecToUInt( new( 0.65f ) ), $"{gaugeData.mGaugeHalfRange_Pct:F1}" );
			ImGui.GetWindowDrawList().AddText( gaugeBase + new Vector2( gaugeSize.X - rightPctTextWidth, gaugeSize.Y / 3f * 2f + 1f ), ImGuiUtils.ColorVecToUInt( new( 0.65f ) ), $"{gaugeData.mGaugeHalfRange_Pct:F1}" );

			
			ImGui.GetWindowDrawList().AddLine(	gaugeBase + new Vector2( gaugeSize.X / 2f - ( gaugeData.mDiffThreshold_Pct * pixelsPerPercent ), 0 ),
												gaugeBase + new Vector2( gaugeSize.X / 2f - ( gaugeData.mDiffThreshold_Pct * pixelsPerPercent ), gaugeSize.Y ),
												ImGuiUtils.ColorVecToUInt( mConfiguration.mThresholdIndicatorColor ), 3f );
			ImGui.GetWindowDrawList().AddLine(	gaugeBase + new Vector2( gaugeSize.X / 2f + ( gaugeData.mDiffThreshold_Pct * pixelsPerPercent ), 0 ),
												gaugeBase + new Vector2( gaugeSize.X / 2f + ( gaugeData.mDiffThreshold_Pct * pixelsPerPercent ), gaugeSize.Y ),
												ImGuiUtils.ColorVecToUInt( mConfiguration.mThresholdIndicatorColor ), 3f );

			string HPDiffText = $"{Math.Abs( gaugeData.HPDiff_Pct ):F1}";
			float HPDiffTextWidth = ImGui.CalcTextSize( HPDiffText ).X;
			ImGui.GetWindowDrawList().AddText( gaugeBase + new Vector2( triangleCenterX - HPDiffTextWidth / 2f, gaugeSize.Y / 3f * 2f + 1f ), ImGuiUtils.ColorVecToUInt( Vector4.One ), HPDiffText );

			ImGui.GetWindowDrawList().AddTriangleFilled( gaugeBase + new Vector2( triangleCenterX - triangleWidth / 2f, 0 ), gaugeBase + new Vector2( triangleCenterX + triangleWidth / 2f, 0 ), gaugeBase + new Vector2( triangleCenterX, triangleHeight ), ImGuiUtils.ColorVecToUInt( triangleColor ) );
			
			
		}

		ImGui.End();
	}

	private readonly Plugin mPlugin;
	private readonly Configuration mConfiguration;

	internal bool mSettingsWindowVisible = false;
	private int mGaugeIndexWantToDelete = -1;
}
