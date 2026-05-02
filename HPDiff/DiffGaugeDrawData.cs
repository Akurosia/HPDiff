using System.Numerics;

namespace HPDiff;

internal class DiffGaugeDrawData
{
	internal bool mShouldDraw;

	internal string mEnemy1Name = string.Empty;
	internal string mEnemy2Name = string.Empty;
	internal Vector4 mLeftColor;
	internal Vector4 mRightColor;
	internal float mGaugeHalfRange_Pct;
	internal float mDiffThreshold_Pct;
	internal float mEnemy1HP_Pct;
	internal float mEnemy2HP_Pct;

	internal float HPDiff_Pct => mEnemy2HP_Pct - mEnemy1HP_Pct;

	internal static readonly DiffGaugeDrawData PreviewGaugeData = new()
	{
		mShouldDraw = true,
		mEnemy1Name = "Nidhogg",
		mEnemy2Name = "Hraesvelgr",
		mLeftColor = new( 163f/255f, 73f/255f, 164f/255f, 1f ),
		mRightColor = new( 153f/255f, 217f/255f, 234f/255f, 1f ),
		mGaugeHalfRange_Pct = 15f,
		mDiffThreshold_Pct = 3f,
		mEnemy1HP_Pct = 50.1f,
		mEnemy2HP_Pct = 47.8f
	};
}
