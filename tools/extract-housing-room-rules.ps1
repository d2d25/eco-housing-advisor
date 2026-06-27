param(
  [string]$HousingValuesPath = "C:\Program Files (x86)\Steam\steamapps\common\Eco Server\Mods\__core__\Systems\HousingValues.cs"
)

$lines = Get-Content -LiteralPath $HousingValuesPath | Where-Object { $_ -match 'new RoomCategory\(\)' }
$categories = foreach ($line in $lines) {
  $name = [regex]::Match($line, 'DisplayName\s*=\s*Localizer\.DoStr\("(?<name>[^"]+)"\)').Groups['name'].Value
  if (-not $name) { continue }

  $supports = @()
  $supportMatch = [regex]::Match($line, 'SupportingRoomCategoryNames\s*=\s*new\[\]\s*\{(?<items>.*?)\}')
  if ($supportMatch.Success) {
    $supports = [regex]::Matches($supportMatch.Groups['items'].Value, '"([^"]+)"') | ForEach-Object { $_.Groups[1].Value }
  }

  [pscustomobject]@{
    Name = $name
    CanBeRoom = -not ($line -match 'CanBeRoomCategory\s*=\s*false')
    SupportForAnyRoomType = $line -match 'SupportForAnyRoomType\s*=\s*true'
    NegatesValue = $line -match 'NegatesValue\s*=\s*true'
    Supports = $supports
  }
}

$rooms = $categories | Where-Object { $_.CanBeRoom -and -not $_.NegatesValue }
$roomNames = $rooms | ForEach-Object { $_.Name }

foreach ($category in $categories) {
  $useful = New-Object System.Collections.Generic.List[string]

  if ($category.CanBeRoom -and -not $category.NegatesValue) {
    $useful.Add($category.Name)
  }

  foreach ($room in $rooms) {
    if ($room.Supports -contains $category.Name -and -not $useful.Contains($room.Name)) {
      $useful.Add($room.Name)
    }
  }

  if ($category.SupportForAnyRoomType) {
    foreach ($roomName in $roomNames) {
      if (-not $useful.Contains($roomName)) {
        $useful.Add($roomName)
      }
    }
  }

  [pscustomobject]@{
    Category = $category.Name
    CanBeRoom = $category.CanBeRoom
    SupportForAnyRoomType = $category.SupportForAnyRoomType
    NegatesValue = $category.NegatesValue
    UsefulIn = $useful -join ', '
    Supports = $category.Supports -join ', '
  }
}
