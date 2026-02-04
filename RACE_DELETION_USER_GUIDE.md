# 🗑️ Guide utilisateur - Suppression de courses

## 📖 Comment supprimer une course dans Race Classification

### 📍 Emplacement
**Onglet**: Race Classification  
**Section**: Races in Event (Ordered by Distance)

---

## 🎯 Étapes simples

### 1️⃣ Ouvrir l'onglet Race Classification
- Cliquez sur l'onglet **"Race Classification"** en haut de l'application

### 2️⃣ Sélectionner l'événement de course
- Dans le menu déroulant **"Select Race Event"**
- Choisissez l'événement contenant la course à supprimer
- Les courses de cet événement s'affichent dans le tableau

### 3️⃣ Sélectionner la course à supprimer
- Cliquez sur la ligne de la course dans le tableau
- La ligne devient surlignée en bleu
- Le bouton **"🗑️ Delete Selected Race"** (rouge) s'active

### 4️⃣ Cliquer sur le bouton Delete
- Cliquez sur le bouton rouge **"🗑️ Delete Selected Race"**
- Une fenêtre de confirmation apparaît

### 5️⃣ Confirmer la suppression
**Message de confirmation:**
```
Are you sure you want to delete race '[Nom de la course]'?
This will also delete all associated classifications.

[Yes]  [No]
```

- Cliquez sur **"Yes"** pour supprimer définitivement
- Cliquez sur **"No"** pour annuler

### 6️⃣ Vérifier la suppression
- Un message de confirmation s'affiche : "Race '[Nom]' deleted successfully."
- La course disparaît de la liste
- Les classifications associées sont également supprimées

---

## ⚠️ Avertissements importants

### 🚨 Suppression définitive
- **Aucune annulation possible** - La suppression est permanente
- **Toutes les classifications sont supprimées** - Les résultats de tous les participants pour cette course
- **Impact sur les challenges** - Si la course fait partie d'un challenge, cela affectera les classements

### 💡 Avant de supprimer, vérifiez :
- [ ] C'est bien la bonne course (vérifiez la distance et le nom)
- [ ] Vous avez une sauvegarde si nécessaire
- [ ] Les utilisateurs du challenge sont informés (si applicable)

---

## 🎨 Repérage visuel

### Bouton Delete désactivé (grisé)
```
┌────────────────────────────────┐
│ 🗑️ Delete Selected Race       │  ← Grisé = Aucune course sélectionnée
│ (bouton désactivé)             │
└────────────────────────────────┘
```

### Bouton Delete activé (rouge vif)
```
┌────────────────────────────────┐
│ 🗑️ Delete Selected Race       │  ← Rouge = Course sélectionnée
│ (bouton actif - cliquable)     │
└────────────────────────────────┘
```

---

## 📋 Exemple pratique

### Scénario : Supprimer une course erronée

**Situation**: Vous avez importé par erreur les résultats d'une course de 10km deux fois.

**Solution:**

1. Allez dans **Race Classification**
2. Sélectionnez l'événement "Run in Liège 2025"
3. Dans la liste, vous voyez :
   ```
   Distance | Race Name              | Race # | Status
   10       | Run in Liège 10km      | 1      | Processed
   10       | Run in Liège 10km      | 1      | Processed  ← Doublon
   21.1     | Run in Liège Semi      | 1      | Processed
   ```
4. Cliquez sur la 2ème ligne (le doublon)
5. Cliquez sur **"🗑️ Delete Selected Race"**
6. Confirmez en cliquant sur **"Yes"**
7. Le doublon disparaît

**Résultat:**
```
Distance | Race Name              | Race # | Status
10       | Run in Liège 10km      | 1      | Processed
21.1     | Run in Liège Semi      | 1      | Processed
```

---

## ❓ FAQ - Questions fréquentes

### Q1: Que se passe-t-il avec les fichiers sources (PDF/Excel) ?
**R**: Les fichiers ne sont **pas supprimés**. Seules les données en base de données sont effacées.

### Q2: Puis-je récupérer une course supprimée ?
**R**: ⚠️ **Non**, la suppression est définitive. Vous devrez réimporter les résultats depuis le fichier source.

### Q3: Les points du challenge sont-ils recalculés automatiquement ?
**R**: Oui, les classements du challenge se basent sur les courses existantes. Après suppression, rechargez le classement pour voir les points mis à jour.

### Q4: Puis-je supprimer plusieurs courses en même temps ?
**R**: Non, actuellement vous ne pouvez supprimer qu'une seule course à la fois.

### Q5: Le bouton Delete est grisé, pourquoi ?
**R**: Vous devez d'abord **sélectionner une course** dans le tableau en cliquant sur une ligne.

### Q6: J'ai cliqué sur Delete par erreur, comment annuler ?
**R**: Cliquez sur **"No"** dans la fenêtre de confirmation. Si vous avez déjà confirmé, réimportez les résultats.

---

## 🛡️ Bonnes pratiques

### ✅ À faire
- ✅ Vérifier deux fois avant de confirmer
- ✅ Sauvegarder les fichiers sources avant suppression
- ✅ Informer les participants du challenge si nécessaire
- ✅ Supprimer les doublons après import
- ✅ Nettoyer les courses de test

### ❌ À éviter
- ❌ Supprimer une course avec de vraies données sans réfléchir
- ❌ Supprimer toutes les courses d'un événement (supprimez plutôt l'événement)
- ❌ Supprimer une course en production sans backup
- ❌ Confondre les courses qui ont des noms similaires

---

## 🔧 Dépannage

### Problème : Le bouton Delete ne s'active pas
**Solutions:**
1. Cliquez bien sur **une ligne complète** dans le tableau des courses
2. Vérifiez qu'un événement est sélectionné
3. Actualisez en cliquant sur "Refresh Races"

### Problème : Message d'erreur lors de la suppression
**Solutions:**
1. Notez le message d'erreur exact
2. Vérifiez que la base de données n'est pas utilisée par une autre application
3. Redémarrez l'application
4. Contactez le support avec le message d'erreur

### Problème : La course reste affichée après suppression
**Solutions:**
1. Cliquez sur "Refresh Races" pour actualiser la liste
2. Si le problème persiste, vérifiez dans l'onglet "Upload and Process" si la course existe toujours

---

## 📞 Support

Si vous rencontrez des problèmes ou avez des questions:
- Consultez la documentation complète: `RACE_DELETION_FEATURE_IMPLEMENTATION.md`
- Vérifiez le statut de la base de données
- Contactez l'administrateur système

---

**Date de création**: 2025-02-09  
**Version du guide**: 1.0
