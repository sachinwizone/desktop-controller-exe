# 🚀 Quick Start Guide: Site Management Features

## Overview

The Site Monitoring dashboard now has complete CRUD (Create, Read, Update, Delete) functionality for managing monitored websites.

---

## 📝 Adding a New Site

### Steps:
1. Click **"Add New Site"** button (top-right of dashboard)
2. Fill in the form:
   - **Site Name**: Give your site a friendly name (e.g., "My API Server")
   - **Site URL**: Enter the full URL (e.g., https://example.com)
   - **Company Name**: Select your company
   - **Check Interval**: How often to check status (10-3600 seconds, default: 30s)
3. Click **"Add Site"** button
4. ✅ **Result**: Site appears in left sidebar immediately
5. ✅ **Bonus**: Status check automatically starts

### Screenshot:
```
┌─────────────────────────────────────┐
│     Add New Site Modal              │
│                                     │
│  Site Name: _____________________   │
│  Site URL:  _____________________   │
│  Company:   [Dropdown]              │
│  Interval:  [30] seconds            │
│                                     │
│  [Cancel]  [Add Site]               │
└─────────────────────────────────────┘
```

---

## ✏️ Editing a Site

### Steps:
1. **Hover** over any site card in the left sidebar
2. Click **Edit button (✎)** that appears
3. Update any fields:
   - Site Name
   - Site URL
   - Check Interval
4. Click **"Save Changes"**
5. ✅ **Result**: Sidebar updates immediately with new values

### What Can Be Edited:
- ✏️ **Site Name** - Your friendly name for the site
- ✏️ **Site URL** - The website address to monitor
- ✏️ **Check Interval** - How frequently to check (10-3600 seconds)

### What Cannot Be Edited:
- ❌ Company Name (changes require deletion & re-adding)
- ❌ Site ID (auto-assigned)
- ❌ Status/Metrics (real-time, calculated)

### Screenshot:
```
Before hover:          After hover:
┌──────────────────┐  ┌──────────────────┐
│ Site Name        │  │ Site Name    [✎][🗑]│
│ example.com      │  │ example.com      │
│ ● ONLINE         │  │ ● ONLINE         │
└──────────────────┘  └──────────────────┘

Edit Modal Opens:
┌─────────────────────────────────────┐
│     Edit Site                       │
│                                     │
│  Site Name: Updated Name ______     │
│  URL:       https://new-url.com ─   │
│  Interval:  [60] seconds            │
│                                     │
│  [Cancel]  [Save Changes]           │
└─────────────────────────────────────┘
```

---

## 🗑️ Deleting a Site

### Steps:
1. **Hover** over any site card in the left sidebar
2. Click **Delete button (🗑)** that appears
3. **Confirmation dialog** appears with site name
4. Review the warning about associated data being deleted
5. Click **"Delete Site"** to confirm
6. ✅ **Result**: Site removed from sidebar
7. ✅ **Bonus**: All associated data cleaned up

### What Gets Deleted:
- 🗑️ The site itself
- 🗑️ All downtime events
- 🗑️ All traffic analytics
- 🗑️ All device/country analytics
- 🗑️ All page analytics

### Important Notes:
- ⚠️ This action **CANNOT be undone**
- ⚠️ All history will be **permanently deleted**
- ⚠️ No backup is created automatically

### Screenshot:
```
Confirmation Dialog:
┌──────────────────────────────────────┐
│         🗑 Delete Site                │
│  ─────────────────────────────────   │
│                                      │
│  Are you sure you want to delete    │
│  "example.com"?                     │
│                                      │
│  This will remove all downtime      │
│  records and monitoring history     │
│                                      │
│  [Cancel]  [Delete Site]             │
└──────────────────────────────────────┘
```

---

## 📊 Monitoring After Operations

### After Adding a Site:
- Site appears in sidebar with status dot
- If status is 🟢 GREEN → Site is ONLINE
- If status is 🔴 RED → Site is OFFLINE
- Dashboard shows live metrics:
  - Response time
  - Uptime percentage
  - Visitor count
  - Page views
  - Load time
  - Real-time charts

### After Editing a Site:
- All changes take effect immediately
- Status check uses new URL
- Check interval updates to new value
- No downtime in monitoring

### After Deleting a Site:
- Site removed from sidebar
- If it was selected, dashboard goes blank
- All historical data is gone
- Other sites continue monitoring normally

---

## 🎯 Common Tasks

### Task 1: Change How Often a Site is Checked
1. Hover over the site card
2. Click edit (✎)
3. Change the "Interval" field
4. Click "Save Changes"
✅ Done! Site will now be checked every N seconds

### Task 2: Update Site URL
1. Hover over the site card
2. Click edit (✎)
3. Update the "Site URL" field
4. Click "Save Changes"
✅ Done! Monitoring continues with new URL

### Task 3: Rename a Site
1. Hover over the site card
2. Click edit (✎)
3. Update the "Site Name" field
4. Click "Save Changes"
✅ Done! Sidebar shows new name

### Task 4: Move Site to Different Company
1. Delete the site (click 🗑, confirm)
2. Add the site again with different company
✅ Done! Site now monitoring under new company

### Task 5: Add Multiple Sites at Once
1. Click "Add New Site"
2. Add first site, click "Add Site"
3. Click "Add New Site" again
4. Repeat for each site
✅ Done! All sites appear in sidebar

---

## ⚙️ Technical Details

### Validation Rules

#### Site Name
- ✅ Any characters allowed
- ✅ Min: 1 character
- ✅ Max: 255 characters
- ✅ Required field

#### Site URL
- ✅ Must start with `http://` or `https://`
- ✅ Must be valid URL format
- ✅ Example: `https://example.com`
- ✅ Required field

#### Check Interval
- ✅ Minimum: 10 seconds
- ✅ Maximum: 3600 seconds (1 hour)
- ✅ Default: 30 seconds
- ✅ Must be a number

### Response Times
| Operation | Time |
|-----------|------|
| Add Site | 1-2 seconds |
| Edit Site | 1-2 seconds |
| Delete Site | 1-2 seconds |
| Sidebar Refresh | < 1 second |
| Status Detection | 5 seconds max |

---

## 🔔 Feedback & Confirmations

### Success Messages (Green Toast)
```
✅ Site added successfully!
✅ Site updated successfully!
✅ Site deleted successfully
```

### Error Messages (Red Toast)
```
❌ Please fill in all fields
❌ Invalid URL format
❌ Interval must be between 10 and 3600 seconds
❌ Site not found
❌ Failed to add/update/delete site
```

### Keyboard Shortcuts
- `Escape` → Close any open modal (Add/Edit/Delete)
- Click outside modal → Close modal
- Tab → Move between form fields
- Enter → Submit form (when in last field)

---

## 🛡️ Data Safety

### Backups
- ✅ No automatic backups (use your database backup)
- ✅ Always confirm before deleting
- ✅ Downtime events are historical records

### Data Isolation
- ✅ Each company sees only their sites
- ✅ Users can only manage their company's sites
- ✅ Admin can see all sites

### Audit Trail
- ⏰ `created_at` - When site was added
- ⏰ `updated_at` - Last time any field was modified
- 📊 Downtime records keep full history

---

## 📱 Mobile Friendly

All features work on mobile devices:
- ✅ Add site form responsive
- ✅ Edit modal responsive
- ✅ Delete confirmation responsive
- ✅ Site cards stack vertically
- ✅ Buttons scale for touch

---

## 🆘 Troubleshooting

### Problem: New site doesn't appear
- **Solution**: Refresh the page (F5)
- **Solution**: Check console for errors (F12)

### Problem: Can't edit a site
- **Solution**: Hover over site to reveal edit button
- **Solution**: Check if you have permission to edit

### Problem: Site URL error on edit
- **Solution**: Make sure URL starts with `http://` or `https://`
- **Solution**: No spaces in URL

### Problem: Interval change not working
- **Solution**: Must be between 10-3600 seconds
- **Solution**: Must be a whole number

### Problem: Delete not working
- **Solution**: Try refreshing page
- **Solution**: Check database connection
- **Solution**: Check user permissions

---

## 📚 Related Features

### View Downtime History
1. Click on any site card
2. Dashboard appears on right
3. Scroll to "Downtime History" section
4. See all offline events with timestamps

### View Analytics
1. Click on any site card
2. Dashboard appears on right
3. See traffic sources, devices, countries
4. Updated in real-time

### Check Live Status
1. Click on any site card
2. Green dot (🟢) = Site is ONLINE
3. Red dot (🔴) = Site is OFFLINE
4. Check response time shown in dashboard

---

## ⚡ Best Practices

### ✅ Do's
- ✅ Use descriptive site names
- ✅ Include port number if non-standard (e.g., https://example.com:8080)
- ✅ Set appropriate check intervals (higher for stable sites)
- ✅ Review sites regularly for relevance
- ✅ Archive old sites by deleting them

### ❌ Don'ts
- ❌ Don't add localhost URLs (won't be accessible)
- ❌ Don't add too many sites with 10-second intervals (high load)
- ❌ Don't forget to confirm deletions
- ❌ Don't edit URLs without updating firewall rules
- ❌ Don't add duplicate URLs

---

## 📞 Support

For issues or feature requests:
1. Check the [COMPLETION_REPORT.md](COMPLETION_REPORT.md) for technical details
2. Review [SITE_MANAGEMENT_SUMMARY.md](SITE_MANAGEMENT_SUMMARY.md) for overview
3. Check browser console for error messages
4. Contact system administrator

---

## 📋 Checklist for Site Setup

When adding a new site, verify:
- [ ] Site name is descriptive
- [ ] URL is correct and accessible
- [ ] Check interval is appropriate
- [ ] Company is selected correctly
- [ ] Site appears in sidebar after adding
- [ ] Status detection starts automatically
- [ ] Dashboard shows metrics
- [ ] Can view downtime history

---

**Version**: 1.0
**Last Updated**: 2024-12-19
**Status**: Production Ready ✅
