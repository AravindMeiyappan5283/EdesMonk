[System.Reflection.Assembly]::LoadWithPartialName('System.Drawing') | Out-Null

$imagePath = 'D:\Projects\EdesMonk\Edesmonk1.png'
$image = [System.Drawing.Image]::FromFile($imagePath)
$bitmap = New-Object System.Drawing.Bitmap($image)

# Make white background transparent
$whiteColor = [System.Drawing.Color]::White
$bitmap.MakeTransparent($whiteColor)

$bitmap.Save($imagePath, [System.Drawing.Imaging.ImageFormat]::Png)
$bitmap.Dispose()
$image.Dispose()

Write-Host "Background removed!"
