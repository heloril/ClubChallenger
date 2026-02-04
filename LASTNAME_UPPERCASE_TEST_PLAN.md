# 📋 Plan de tests - Noms de famille en MAJUSCULES

## 🎯 Objectif
Vérifier que tous les noms de famille sont affichés en **MAJUSCULES** dans tous les rapports et emails.

---

## ✅ Checklist de tests

### 📧 1. Tests des emails de Challenge Mailing

#### Test 1.1: Génération du template d'email
- [ ] Ouvrir l'onglet "Challenge Mailing"
- [ ] Sélectionner un challenge
- [ ] Cliquer sur "Generate Template"
- [ ] **Vérifier**: Section "Derniers Résultats" - Les noms doivent être "Prénom NOM"
  - Exemple attendu: `Jean DUPONT`
- [ ] **Vérifier**: Section "Classement Actuel du Challenge" - Les noms doivent être "Prénom NOM"
  - Exemple attendu: `Marie MARTIN`

#### Test 1.2: Envoi d'email de test
- [ ] Entrer une adresse email de test
- [ ] Cliquer sur "Send Test Email"
- [ ] Ouvrir l'email reçu
- [ ] **Vérifier**: Tous les noms dans les tableaux sont en format "Prénom NOM"

#### Test 1.3: Aperçu HTML dans l'éditeur
- [ ] Après génération du template, vérifier l'aperçu
- [ ] **Vérifier**: Les noms apparaissent correctement formatés

---

### 📄 2. Tests des exports de classification de course

#### Test 2.1: Export HTML (simple)
- [ ] Sélectionner une course dans l'onglet "Race Classification"
- [ ] Cliquer sur "Export" → "HTML"
- [ ] Ouvrir le fichier HTML généré
- [ ] **Vérifier**: Colonne "Nom" affiche "Prénom NOM"

**Exemple attendu dans le fichier HTML:**
```html
<td>Jean DUPONT</td>
```

#### Test 2.2: Export Texte (TXT)
- [ ] Sélectionner une course
- [ ] Cliquer sur "Export" → "Text"
- [ ] Ouvrir le fichier TXT généré
- [ ] **Vérifier**: Les noms dans le tableau sont "Prénom NOM"

**Exemple attendu:**
```
Rank│ Position│ Name                         │ Team                │ RaceTime
1   │ 1       │ Jean DUPONT                  │ TTRS               │ 00:45:23
```

---

### 📊 3. Tests des exports multi-courses (Race Event)

#### Test 3.1: Export HTML (événement complet)
- [ ] Aller dans l'onglet "Challenger Classification"
- [ ] Section "Export Multiple Races"
- [ ] Sélectionner un événement
- [ ] Cliquer sur "Export to HTML"
- [ ] Ouvrir le fichier HTML
- [ ] **Vérifier**: Pour chaque course, les noms sont en "Prénom NOM"

#### Test 3.2: Export Excel (XLSX)
- [ ] Sélectionner un événement
- [ ] Cliquer sur "Export to Excel"
- [ ] Ouvrir le fichier Excel
- [ ] **Vérifier**: Chaque onglet (par distance) affiche la colonne "Last Name" en MAJUSCULES

**Vérification dans Excel:**
| Position | First Name | Last Name | Points |
|----------|------------|-----------|--------|
| 1        | Jean       | DUPONT    | 100    |
| 2        | Marie      | MARTIN    | 95     |

#### Test 3.3: Export Word (DOCX)
- [ ] Sélectionner un événement
- [ ] Cliquer sur "Export to Word"
- [ ] Ouvrir le fichier Word
- [ ] **Vérifier**: Les tableaux affichent les noms en "Prénom NOM"

#### Test 3.4: Export Summary (TXT)
- [ ] Sélectionner un événement
- [ ] Cliquer sur "Export Summary"
- [ ] Ouvrir le fichier TXT
- [ ] **Vérifier**: Top 10 affiche les noms en "Prénom NOM"

**Exemple attendu:**
```
🥇   1. Jean DUPONT                  00:45:23 👤⭐
🥈   2. Marie MARTIN                 00:46:12 👤⭐
🥉   3. Pierre BERNARD               00:47:05 👤
```

---

### 🏆 4. Tests des exports de classement Challengers

#### Test 4.1: Export HTML - Résumé
- [ ] Dans l'onglet "Challenger Classification"
- [ ] Sélectionner un challenge
- [ ] Cliquer sur "Export Summary to HTML"
- [ ] Ouvrir le fichier HTML
- [ ] **Vérifier**: Colonne "Name" affiche "Prénom NOM"

#### Test 4.2: Export Excel - Résumé
- [ ] Cliquer sur "Export Summary to Excel"
- [ ] Ouvrir le fichier Excel
- [ ] **Vérifier**: Colonne "Name" affiche "Prénom NOM"

**Vérification dans Excel:**
| Rank | Name           | Total Points | Total Races |
|------|----------------|--------------|-------------|
| 1    | Jean DUPONT    | 500          | 5           |
| 2    | Marie MARTIN   | 475          | 5           |

#### Test 4.3: Export Word - Résumé
- [ ] Cliquer sur "Export Summary to Word"
- [ ] Ouvrir le fichier Word
- [ ] **Vérifier**: Tableau avec noms en "Prénom NOM"

#### Test 4.4: Export Excel - Détaillé
- [ ] Cliquer sur "Export Detailed View to Excel"
- [ ] Ouvrir le fichier Excel
- [ ] **Vérifier**: Chaque onglet (un par challenger) a le nom en "Prénom NOM"
- [ ] **Vérifier**: Dans chaque feuille, le titre affiche "Prénom NOM"

**Vérification:**
- Nom de l'onglet: "Jean DUPONT" (ou tronqué si > 31 caractères)
- Titre dans la feuille: "Jean DUPONT"

---

## 📋 Cas de tests spécifiques

### Test avec différents formats de noms

| Nom original | Format attendu |
|--------------|----------------|
| Lamberty     | LAMBERTY       |
| Van Larken   | VAN LARKEN     |
| De Vos       | DE VOS         |
| Pardo Garcia | PARDO GARCIA   |

### Test avec caractères spéciaux

| Nom original | Format attendu |
|--------------|----------------|
| Léga         | LÉGA           |
| Kéris        | KÉRIS          |
| Szwajkajzer  | SZWAJKAJZER    |

---

## 🔍 Points de vérification détaillés

### Pour les emails
- [ ] Les tableaux HTML ont des balises `<td>` avec format "Prénom NOM"
- [ ] Le style CSS n'interfère pas avec la casse
- [ ] L'aperçu dans le RichTextBox affiche correctement

### Pour les exports HTML
- [ ] Les cellules de tableau contiennent bien le texte en majuscules
- [ ] Le rendu dans le navigateur est correct
- [ ] L'impression PDF préserve la casse

### Pour les exports Excel
- [ ] Les cellules affichent les majuscules (pas une formule)
- [ ] Le format de cellule est "Texte" ou "Général"
- [ ] Le copier-coller préserve la casse

### Pour les exports Word
- [ ] Les cellules de tableau contiennent le texte en majuscules
- [ ] La police n'est pas en "small caps" (petites capitales)
- [ ] L'export PDF préserve la casse

---

## 🐛 Tests de régression

### Vérifier que rien n'est cassé

- [ ] Les filtres (Members/Challengers) fonctionnent toujours
- [ ] Le tri par colonne fonctionne
- [ ] Les totaux et statistiques sont corrects
- [ ] La recherche de participants fonctionne
- [ ] Les couleurs et mise en forme sont préservées

---

## 📊 Rapport de tests

### Résultats attendus

✅ **100% des noms de famille doivent être en MAJUSCULES** dans :
- Tous les emails générés
- Tous les exports HTML
- Tous les exports Excel
- Tous les exports Word
- Tous les exports texte

### Critères de succès

| Critère | Statut |
|---------|--------|
| Emails - Résultats de course | ⏳ À tester |
| Emails - Classement challenge | ⏳ À tester |
| Export HTML simple | ⏳ À tester |
| Export Texte | ⏳ À tester |
| Export HTML multi-courses | ⏳ À tester |
| Export Excel multi-courses | ⏳ À tester |
| Export Word multi-courses | ⏳ À tester |
| Export Summary TXT | ⏳ À tester |
| Export Challenger HTML | ⏳ À tester |
| Export Challenger Excel | ⏳ À tester |
| Export Challenger Word | ⏳ À tester |
| Export Challenger Détaillé | ⏳ À tester |

**Légende:**
- ⏳ À tester
- ✅ Testé et validé
- ❌ Testé avec problèmes
- 🔧 Corrigé et à re-tester

---

## 🚀 Scénarios de tests complets

### Scénario 1: Workflow complet d'un mailing
1. Sélectionner "Challenge 2025"
2. Générer le template
3. Vérifier l'aperçu HTML
4. Envoyer à une adresse de test
5. Vérifier l'email reçu
6. **Résultat attendu**: Tous les noms en "Prénom NOM"

### Scénario 2: Export d'un événement complet
1. Sélectionner "Run in Liège 2025"
2. Exporter en Excel
3. Vérifier chaque onglet (chaque distance)
4. **Résultat attendu**: Colonne LastName en MAJUSCULES

### Scénario 3: Classement annuel des challengers
1. Sélectionner "Challenge 2025"
2. Exporter le résumé en HTML
3. Exporter la vue détaillée en Excel
4. **Résultat attendu**: Tous les noms en "Prénom NOM"

---

## 📝 Notes de test

### Environnement de test
- **Application**: ClubChallenger (NameParser.UI)
- **Version .NET**: .NET 8
- **Date de test**: _____________
- **Testeur**: _____________

### Observations
_Notes durant les tests:_
- 
- 
- 

### Bugs trouvés
_Liste des problèmes identifiés:_
- 
- 
- 

### Suggestions d'amélioration
_Idées pour améliorer l'implémentation:_
- 
- 
- 

---

**Date de création**: 2025-02-09
**Version du plan de test**: 1.0
