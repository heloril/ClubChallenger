# Function to normalize strings (remove accents, lowercase)
function Normalize-String {
    param([string]$text)
    if ([string]::IsNullOrWhiteSpace($text)) { return "" }
    
    $normalized = $text.ToLowerInvariant().Trim()
    $normalized = $normalized -replace '[àáâãäå]', 'a'
    $normalized = $normalized -replace '[èéêë]', 'e'
    $normalized = $normalized -replace '[ìíîï]', 'i'
    $normalized = $normalized -replace '[òóôõö]', 'o'
    $normalized = $normalized -replace '[ùúûü]', 'u'
    $normalized = $normalized -replace '[ýÿ]', 'y'
    $normalized = $normalized -replace '[ç]', 'c'
    $normalized = $normalized -replace '[ñ]', 'n'
    $normalized = $normalized -replace '\s+', ' '
    $normalized = $normalized -replace '[^a-z0-9 @\.\-_]', ''
    return $normalized
}

# Read existing JSON
$existingJson = Get-Content "NameParser\Challenge.json" -Raw | ConvertFrom-Json
$existingList = @($existingJson)

# Create a hashtable for quick lookup (normalized)
$existingKeys = @{}
foreach ($entry in $existingList) {
    $normalizedEmail = Normalize-String $entry.Email
    $normalizedFirstName = Normalize-String $entry.FirstName
    $normalizedLastName = Normalize-String $entry.LastName
    
    $key1 = "$normalizedEmail"
    $key2 = "$normalizedFirstName|$normalizedLastName"
    
    if (-not [string]::IsNullOrWhiteSpace($normalizedEmail)) {
        $existingKeys[$key1] = $true
    }
    if (-not [string]::IsNullOrWhiteSpace($normalizedFirstName) -and -not [string]::IsNullOrWhiteSpace($normalizedLastName)) {
        $existingKeys[$key2] = $true
    }
}

# New entries to add (from user's data - updating year 2026 to 2025)
$newEntries = @(
    @{ Timestamp="2025-01-26T19:01:25"; FirstName="Helori"; LastName="Lamberty"; DateOfBirth="1983-11-12"; Email="heloril@outlook.com"; Phone="0499677452"; Team="Hors Stade" },
    @{ Timestamp="2025-01-26T19:01:32"; FirstName="Ludivine"; LastName="Vandeweyer"; DateOfBirth="1978-08-01"; Email="vandeweyerludivine@gmail.com"; Phone="0479794438"; Team="Hors Stade" },
    @{ Timestamp="2025-01-26T19:08:49"; FirstName="Marc"; LastName="Henry"; DateOfBirth="1967-05-29"; Email="mhenry2905@gmail.com"; Phone=""; Team="TIMS" },
    @{ Timestamp="2025-01-26T19:09:57"; FirstName="GIUSEPPE"; LastName="PUMA"; DateOfBirth="1981-11-08"; Email="Joseph.puma81@gmail.com"; Phone=""; Team="Hors Stade" },
    @{ Timestamp="2025-01-26T19:11:28"; FirstName="Sébastien"; LastName="Duhr"; DateOfBirth="1979-11-19"; Email="sebastien.duhr@gmail.com"; Phone="0496495624"; Team="La grinta" },
    @{ Timestamp="2025-01-26T19:17:13"; FirstName="Daniel"; LastName="Van Larken"; DateOfBirth="1969-09-15"; Email="daniel.immoans@gmail.com"; Phone="0475729117"; Team="TTRS" },
    @{ Timestamp="2025-01-26T19:32:35"; FirstName="Théo"; LastName="Marien"; DateOfBirth="1963-07-23"; Email="vr46@outlook.be"; Phone="0494842993"; Team="RUN IN LIÈGE" },
    @{ Timestamp="2025-01-26T19:53:31"; FirstName="Jean marc"; LastName="Cuccuru"; DateOfBirth="1965-02-06"; Email="Jean.marc.cuccuru@outlook.be"; Phone="0498197539"; Team="Teams club l anpsinois" },
    @{ Timestamp="2025-01-26T20:00:12"; FirstName="Florence"; LastName="Somja"; DateOfBirth="1984-05-09"; Email="florencesomja@hotmail.com"; Phone="0477393122"; Team="Hors Stade" },
    @{ Timestamp="2025-01-26T20:02:12"; FirstName="Ludovic"; LastName="Marchal"; DateOfBirth="1973-04-22"; Email="ludo.marchal@gmail.com"; Phone="478275617"; Team="Hors Stade" },
    @{ Timestamp="2025-01-26T20:02:25"; FirstName="Hugues"; LastName="Botquin"; DateOfBirth="1966-05-28"; Email="huguesbotquin@gmail.com"; Phone="0473228660"; Team="Athois d'courir" },
    @{ Timestamp="2025-01-26T20:02:39"; FirstName="Massimo"; LastName="Ragusa"; DateOfBirth="1984-11-08"; Email="ragusa-massimo@hotmail.com"; Phone="0485 661 656"; Team="Hors Stade" },
    @{ Timestamp="2025-01-26T20:03:07"; FirstName="Daniel"; LastName="Klarzynski"; DateOfBirth="1985-03-12"; Email="danielklarzynski10@gmail.com"; Phone="0491305923"; Team="Masters seraing" },
    @{ Timestamp="2025-01-26T20:13:18"; FirstName="Patrick"; LastName="Tixhon"; DateOfBirth="1971-03-23"; Email="patrick.tixhon@live.be"; Phone="0492 79 80 30"; Team="SERAING RUNNERS" },
    @{ Timestamp="2025-01-26T20:51:39"; FirstName="Claude"; LastName="DELHEZ"; DateOfBirth="1960-09-12"; Email="claudelhe@yahoo.fr"; Phone="0498398506"; Team="Run In Liège" },
    @{ Timestamp="2025-01-26T20:58:31"; FirstName="ERICA"; LastName="RASCHELLA"; DateOfBirth="1971-02-23"; Email="erica_raschella@hotmail.com"; Phone="+32478263336"; Team="" },
    @{ Timestamp="2025-01-26T21:03:57"; FirstName="Juan"; LastName="Pardo Garcia"; DateOfBirth="1964-03-15"; Email="juanpardogarcia@gmail.com"; Phone="0495819726"; Team="Hors Stade" },
    @{ Timestamp="2025-01-26T21:04:30"; FirstName="Pascal"; LastName="Lambotte"; DateOfBirth="1967-03-28"; Email="lambottep@gmail.com"; Phone="0474393644"; Team="Hors Stade" },
    @{ Timestamp="2025-01-26T21:42:27"; FirstName="Monique"; LastName="Keris"; DateOfBirth="1964-01-06"; Email="moniquekeris@yahoo.fr"; Phone="0475742468"; Team="Run In Liege" },
    @{ Timestamp="2025-01-26T23:02:45"; FirstName="Jean-Marie"; LastName="Gheury"; DateOfBirth="1968-07-19"; Email="jmgheury@gmail.com"; Phone="0479279181"; Team="Hors Stade" },
    @{ Timestamp="2025-01-27T08:40:42"; FirstName="Rosario"; LastName="Scifo"; DateOfBirth="1971-06-29"; Email="rosario.scifo21@gmail.com"; Phone="0496373700"; Team="Hors Stade" },
    @{ Timestamp="2025-01-27T08:43:38"; FirstName="kamal"; LastName="Azzouz"; DateOfBirth="1975-02-04"; Email="zigkam11@live.fr"; Phone=""; Team="Hors Stade" },
    @{ Timestamp="2025-01-27T10:13:53"; FirstName="Victor"; LastName="Léon"; DateOfBirth="1955-05-20"; Email="victorleon5@hotmail.com"; Phone="0489779468"; Team="" },
    @{ Timestamp="2025-01-27T10:23:50"; FirstName="Daniel"; LastName="DEJONG"; DateOfBirth="1966-11-01"; Email="daniel.dejong@resa.be"; Phone="0495595707"; Team="TTRS" },
    @{ Timestamp="2025-01-27T10:26:49"; FirstName="Victor"; LastName="Léon"; DateOfBirth="1955-05-20"; Email="victorleon5@hotmail.com"; Phone="0486779468"; Team="" },
    @{ Timestamp="2025-01-27T10:54:01"; FirstName="Thibaut"; LastName="Thomassin"; DateOfBirth="1981-09-30"; Email="thibaut_thomassin@hotmail.com"; Phone="0472522410"; Team="DD Team" },
    @{ Timestamp="2025-01-27T11:25:59"; FirstName="Alain"; LastName="Marchal"; DateOfBirth="1965-02-12"; Email="Alainmarchal65@gmail.com"; Phone="0492509818"; Team="Team Club Ampsinois" },
    @{ Timestamp="2025-01-27T11:30:26"; FirstName="Jean-Marie"; LastName="Chudyba"; DateOfBirth="1968-12-26"; Email="jm.chudyba@gmail.com"; Phone="0498 78 39 40"; Team="RUN IN LIÈGE" },
    @{ Timestamp="2025-01-27T13:51:13"; FirstName="Michel"; LastName="Mancini"; DateOfBirth="1947-08-09"; Email="mancini.michel@gmail.com"; Phone="0496472326"; Team="Hors Stade" },
    @{ Timestamp="2025-01-27T14:22:19"; FirstName="Rémi"; LastName="Fabry"; DateOfBirth="1986-09-25"; Email="remi_fabry@hotmail.com"; Phone="0495884958"; Team="Hors Stade" },
    @{ Timestamp="2025-01-27T19:06:51"; FirstName="Freddy"; LastName="Delsaux"; DateOfBirth="1964-11-20"; Email="freddy.delsaux@gmail.com"; Phone="0474394943"; Team="Seraing athlétisme hors Dossard" },
    @{ Timestamp="2025-01-28T15:51:31"; FirstName="Laurent"; LastName="Maes"; DateOfBirth="1980-04-14"; Email="laurent.maes80@gmail.com"; Phone="0477437842"; Team="Casa" },
    @{ Timestamp="2025-01-29T08:36:54"; FirstName="Céline"; LastName="Léga"; DateOfBirth="1977-02-28"; Email="celinelega810@gmail.com"; Phone="0476/65.76.22"; Team="Hors Stade" },
    @{ Timestamp="2025-01-30T11:18:15"; FirstName="Michel"; LastName="Souplet"; DateOfBirth="1961-10-23"; Email="soupletmichel@hotmail.com"; Phone="0473363603"; Team="Serunners" },
    @{ Timestamp="2025-01-31T17:06:57"; FirstName="Claude"; LastName="Delhez"; DateOfBirth="1960-09-12"; Email="claudelhe@yahoo.fr"; Phone="0498 39 85 06"; Team="Run In Liège" },
    @{ Timestamp="2025-02-02T14:12:24"; FirstName="Lionel"; LastName="Catoul"; DateOfBirth="1982-08-26"; Email="lionel.catoul@gmail.com"; Phone="0479802206"; Team="Run in Liège" },
    @{ Timestamp="2025-02-02T16:56:34"; FirstName="Francoise"; LastName="Piscart"; DateOfBirth="1971-10-13"; Email="gaufausuc@gmail.com"; Phone="0498 237475"; Team="Hors Stade" },
    @{ Timestamp="2025-02-03T22:42:41"; FirstName="kalid"; LastName="Lamchachti"; DateOfBirth="1974-06-22"; Email="Kalidoa@hotmail.com"; Phone="0495683040"; Team="RFAC" },
    @{ Timestamp="2025-02-04T16:32:51"; FirstName="Josiane"; LastName="Stiernon"; DateOfBirth="1957-11-27"; Email="josiane.stiernon57@gmail.com"; Phone="0479541362"; Team="Hors Stade" },
    @{ Timestamp="2025-02-04T17:07:27"; FirstName="Robert"; LastName="Dupont"; DateOfBirth="1959-06-06"; Email="robert.dupont@scarlet.be"; Phone="0475537549"; Team="Hors Stade" },
    @{ Timestamp="2025-02-04T22:16:31"; FirstName="Monique"; LastName="Keris"; DateOfBirth="1964-01-06"; Email="moniquekeris@yahoo.fr"; Phone="0475742468"; Team="DD Team" },
    @{ Timestamp="2025-02-04T22:41:50"; FirstName="Bert"; LastName="ERNEST"; DateOfBirth="1964-01-16"; Email="890bsd@gmail.com"; Phone=""; Team="" },
    @{ Timestamp="2025-02-05T04:03:12"; FirstName="Cindy"; LastName="Simon"; DateOfBirth="1983-11-09"; Email="cindsimon@gmail.com"; Phone="0474391561"; Team="Hors Stade" },
    @{ Timestamp="2025-02-05T14:15:51"; FirstName="Michelle"; LastName="Burhenne"; DateOfBirth="1970-08-12"; Email="m.burhenne@skynet.be"; Phone=""; Team="Run in Liège" }
)

# Check for duplicates and add new entries
$newList = @()
$addedCount = 0
$skippedCount = 0

foreach ($entry in $newEntries) {
    $normalizedEmail = Normalize-String $entry.Email
    $normalizedFirstName = Normalize-String $entry.FirstName
    $normalizedLastName = Normalize-String $entry.LastName
    
    $key1 = "$normalizedEmail"
    $key2 = "$normalizedFirstName|$normalizedLastName"
    
    $isDuplicate = $false
    
    if (-not [string]::IsNullOrWhiteSpace($normalizedEmail) -and $existingKeys.ContainsKey($key1)) {
        $isDuplicate = $true
        Write-Host "Skipping duplicate (email): $($entry.FirstName) $($entry.LastName) - $($entry.Email)" -ForegroundColor Yellow
    }
    elseif (-not [string]::IsNullOrWhiteSpace($normalizedFirstName) -and -not [string]::IsNullOrWhiteSpace($normalizedLastName) -and $existingKeys.ContainsKey($key2)) {
        $isDuplicate = $true
        Write-Host "Skipping duplicate (name): $($entry.FirstName) $($entry.LastName)" -ForegroundColor Yellow
    }
    
    if (-not $isDuplicate) {
        # Format the new entry
        $phone = if ([string]::IsNullOrWhiteSpace($entry.Phone)) { $null } else { $entry.Phone -replace '\s+', '' -replace '/', '' }
        $team = if ([string]::IsNullOrWhiteSpace($entry.Team)) { $null } else { $entry.Team }
        
        $newEntry = [PSCustomObject]@{
            "Timestamp" = $entry.Timestamp
            "FirstName" = $entry.FirstName
            "LastName" = $entry.LastName
            "Date de naissance" = "$($entry.DateOfBirth)T00:00:00"
            "Email" = $entry.Email
            "Téléphone" = $phone
            "Team" = $team
            "Réglement" = "Je certifie avoir lu et accepté le règlement du challenge"
            "Comments" = $null
        }
        
        $newList += $newEntry
        Write-Host "Adding: $($entry.FirstName) $($entry.LastName) - $($entry.Email)" -ForegroundColor Green
        $addedCount++
        
        # Update keys to prevent duplicates within the new batch
        if (-not [string]::IsNullOrWhiteSpace($normalizedEmail)) {
            $existingKeys[$key1] = $true
        }
        if (-not [string]::IsNullOrWhiteSpace($normalizedFirstName) -and -not [string]::IsNullOrWhiteSpace($normalizedLastName)) {
            $existingKeys[$key2] = $true
        }
    }
    else {
        $skippedCount++
    }
}

# Combine existing and new entries
$finalList = $existingList + $newList

# Sort by Timestamp
$finalList = $finalList | Sort-Object { [datetime]$_.Timestamp }

# Save to JSON
$json = $finalList | ConvertTo-Json -Depth 10
Set-Content -Path "NameParser\Challenge.json" -Value $json -Encoding UTF8

Write-Host "`n========================================" -ForegroundColor Cyan
Write-Host "Summary:" -ForegroundColor Cyan
Write-Host "  Existing entries: $($existingList.Count)" -ForegroundColor Cyan
Write-Host "  New entries added: $addedCount" -ForegroundColor Green
Write-Host "  Duplicates skipped: $skippedCount" -ForegroundColor Yellow
Write-Host "  Total entries: $($finalList.Count)" -ForegroundColor Cyan
Write-Host "========================================`n" -ForegroundColor Cyan
