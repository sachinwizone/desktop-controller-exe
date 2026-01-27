import React, { useState, useEffect, useRef } from 'react';
import { Activity, Plus, Play, Trash2, RefreshCw, AlertCircle, CheckCircle, Clock, TrendingUp, Server, ExternalLink, Eye } from 'lucide-react';

export default function WebsiteMonitor() {
  const [sites, setSites] = useState([
    {
      id: 'demo-1',
      siteName: 'Google',
      siteUrl: 'https://www.google.com',
      companyName: 'Google LLC',
      interval: 30,
      currentStatus: 'up',
      uptime: 99.8,
      responseTime: 145,
      history: [
        { status: 'up', responseTime: 132, timestamp: new Date(Date.now() - 180000).toISOString() },
        { status: 'up', responseTime: 158, timestamp: new Date(Date.now() - 150000).toISOString() },
        { status: 'up', responseTime: 141, timestamp: new Date(Date.now() - 120000).toISOString() },
        { status: 'up', responseTime: 167, timestamp: new Date(Date.now() - 90000).toISOString() },
        { status: 'up', responseTime: 139, timestamp: new Date(Date.now() - 60000).toISOString() },
        { status: 'up', responseTime: 145, timestamp: new Date(Date.now() - 30000).toISOString() }
      ],
      downtimes: [
        {
          start: new Date(Date.now() - 7200000).toISOString(),
          end: new Date(Date.now() - 7140000).toISOString(),
          duration: 60000
        }
      ],
      totalDowntime: 60000,
      lastChecked: new Date().toISOString(),
      createdAt: new Date(Date.now() - 86400000).toISOString()
    }
  ]);
  const [showAddForm, setShowAddForm] = useState(false);
  const [formData, setFormData] = useState({
    siteName: '',
    siteUrl: '',
    companyName: '',
    interval: 30
  });
  const [monitoring, setMonitoring] = useState({});
  const [previewSite, setPreviewSite] = useState(null);
  const intervalRefs = useRef({});

  const checkSiteStatus = async (siteId, url) => {
    const startTime = Date.now();
    try {
      const response = await fetch(`https://api.allorigins.win/raw?url=${encodeURIComponent(url)}`, {
        method: 'HEAD',
        mode: 'cors'
      });
      const responseTime = Date.now() - startTime;
      const isUp = response.ok;
      
      return {
        status: isUp ? 'up' : 'down',
        responseTime: responseTime,
        timestamp: new Date().toISOString(),
        statusCode: response.status
      };
    } catch (error) {
      const responseTime = Date.now() - startTime;
      return {
        status: 'down',
        responseTime: responseTime,
        timestamp: new Date().toISOString(),
        error: error.message
      };
    }
  };

  const updateSiteMonitoring = (siteId, statusData) => {
    setSites(prevSites => prevSites.map(site => {
      if (site.id === siteId) {
        const history = [...(site.history || []), statusData].slice(-50);
        const wasDown = site.currentStatus === 'down';
        const isNowUp = statusData.status === 'up';
        const wasUp = site.currentStatus === 'up';
        const isNowDown = statusData.status === 'down';

        let updates = {
          currentStatus: statusData.status,
          lastChecked: statusData.timestamp,
          responseTime: statusData.responseTime,
          history: history
        };

        if (wasDown && isNowUp) {
          const downtime = {
            start: site.lastDowntime?.start || site.lastChecked,
            end: statusData.timestamp,
            duration: Date.now() - new Date(site.lastDowntime?.start || site.lastChecked).getTime()
          };
          updates.downtimes = [...(site.downtimes || []), downtime];
          updates.lastDowntime = null;
          updates.totalDowntime = (site.totalDowntime || 0) + downtime.duration;
        } else if (wasUp && isNowDown) {
          updates.lastDowntime = { start: statusData.timestamp };
        } else if (isNowDown && site.lastDowntime) {
          updates.lastDowntime = site.lastDowntime;
        }

        const upCount = history.filter(h => h.status === 'up').length;
        updates.uptime = history.length > 0 ? (upCount / history.length * 100).toFixed(2) : 100;

        return { ...site, ...updates };
      }
      return site;
    }));
  };

  const startMonitoring = (site) => {
    if (intervalRefs.current[site.id]) return;

    const monitor = async () => {
      const statusData = await checkSiteStatus(site.id, site.siteUrl);
      updateSiteMonitoring(site.id, statusData);
    };

    monitor();
    intervalRefs.current[site.id] = setInterval(monitor, site.interval * 1000);
    setMonitoring(prev => ({ ...prev, [site.id]: true }));
  };

  const stopMonitoring = (siteId) => {
    if (intervalRefs.current[siteId]) {
      clearInterval(intervalRefs.current[siteId]);
      delete intervalRefs.current[siteId];
      setMonitoring(prev => ({ ...prev, [siteId]: false }));
    }
  };

  const handleAddSite = () => {
    if (!formData.siteName || !formData.siteUrl || !formData.companyName) return;
    
    const newSite = {
      id: Date.now().toString(),
      ...formData,
      currentStatus: 'unknown',
      uptime: 100,
      history: [],
      downtimes: [],
      totalDowntime: 0,
      createdAt: new Date().toISOString()
    };
    setSites([...sites, newSite]);
    setFormData({ siteName: '', siteUrl: '', companyName: '', interval: 30 });
    setShowAddForm(false);
  };

  const handleTestSite = async (site) => {
    const statusData = await checkSiteStatus(site.id, site.siteUrl);
    updateSiteMonitoring(site.id, statusData);
  };

  const deleteSite = (siteId) => {
    stopMonitoring(siteId);
    setSites(sites.filter(s => s.id !== siteId));
  };

  const formatDuration = (ms) => {
    const seconds = Math.floor(ms / 1000);
    const minutes = Math.floor(seconds / 60);
    const hours = Math.floor(minutes / 60);
    if (hours > 0) return `${hours}h ${minutes % 60}m`;
    if (minutes > 0) return `${minutes}m ${seconds % 60}s`;
    return `${seconds}s`;
  };

  const calculateStats = () => {
    const totalSites = sites.length;
    const upSites = sites.filter(s => s.currentStatus === 'up').length;
    const downSites = sites.filter(s => s.currentStatus === 'down').length;
    const avgResponseTime = sites.length > 0
      ? sites.reduce((acc, s) => acc + (s.responseTime || 0), 0) / sites.length
      : 0;
    return { totalSites, upSites, downSites, avgResponseTime };
  };

  const stats = calculateStats();

  useEffect(() => {
    return () => {
      Object.keys(intervalRefs.current).forEach(id => {
        clearInterval(intervalRefs.current[id]);
      });
    };
  }, []);

  return (
    <div className="min-h-screen bg-gradient-to-br from-slate-900 via-slate-800 to-slate-900 text-white p-6">
      <div className="max-w-7xl mx-auto">
        {/* Header */}
        <div className="flex items-center justify-between mb-8">
          <div className="flex items-center gap-3">
            <Activity className="w-8 h-8 text-blue-400" />
            <h1 className="text-3xl font-bold">Website Monitoring System</h1>
          </div>
          <button
            onClick={() => setShowAddForm(!showAddForm)}
            className="flex items-center gap-2 bg-blue-600 hover:bg-blue-700 px-4 py-2 rounded-lg transition"
          >
            <Plus className="w-5 h-5" />
            Add Site
          </button>
        </div>

        {/* Stats Dashboard */}
        <div className="grid grid-cols-1 md:grid-cols-4 gap-4 mb-8">
          <div className="bg-slate-800/50 backdrop-blur border border-slate-700 rounded-xl p-6">
            <div className="flex items-center justify-between">
              <div>
                <p className="text-slate-400 text-sm">Total Sites</p>
                <p className="text-3xl font-bold mt-1">{stats.totalSites}</p>
              </div>
              <Server className="w-10 h-10 text-blue-400 opacity-50" />
            </div>
          </div>
          <div className="bg-slate-800/50 backdrop-blur border border-slate-700 rounded-xl p-6">
            <div className="flex items-center justify-between">
              <div>
                <p className="text-slate-400 text-sm">Sites Up</p>
                <p className="text-3xl font-bold mt-1 text-green-400">{stats.upSites}</p>
              </div>
              <CheckCircle className="w-10 h-10 text-green-400 opacity-50" />
            </div>
          </div>
          <div className="bg-slate-800/50 backdrop-blur border border-slate-700 rounded-xl p-6">
            <div className="flex items-center justify-between">
              <div>
                <p className="text-slate-400 text-sm">Sites Down</p>
                <p className="text-3xl font-bold mt-1 text-red-400">{stats.downSites}</p>
              </div>
              <AlertCircle className="w-10 h-10 text-red-400 opacity-50" />
            </div>
          </div>
          <div className="bg-slate-800/50 backdrop-blur border border-slate-700 rounded-xl p-6">
            <div className="flex items-center justify-between">
              <div>
                <p className="text-slate-400 text-sm">Avg Response</p>
                <p className="text-3xl font-bold mt-1">{stats.avgResponseTime.toFixed(0)}ms</p>
              </div>
              <TrendingUp className="w-10 h-10 text-purple-400 opacity-50" />
            </div>
          </div>
        </div>

        {/* Add Site Form */}
        {showAddForm && (
          <div className="bg-slate-800/50 backdrop-blur border border-slate-700 rounded-xl p-6 mb-8">
            <h2 className="text-xl font-semibold mb-4">Add New Website</h2>
            <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
              <input
                type="text"
                placeholder="Site Name"
                value={formData.siteName}
                onChange={(e) => setFormData({ ...formData, siteName: e.target.value })}
                className="bg-slate-900 border border-slate-600 rounded-lg px-4 py-2 focus:outline-none focus:border-blue-500"
              />
              <input
                type="url"
                placeholder="Site URL (https://example.com)"
                value={formData.siteUrl}
                onChange={(e) => setFormData({ ...formData, siteUrl: e.target.value })}
                className="bg-slate-900 border border-slate-600 rounded-lg px-4 py-2 focus:outline-none focus:border-blue-500"
              />
              <input
                type="text"
                placeholder="Company Name"
                value={formData.companyName}
                onChange={(e) => setFormData({ ...formData, companyName: e.target.value })}
                className="bg-slate-900 border border-slate-600 rounded-lg px-4 py-2 focus:outline-none focus:border-blue-500"
              />
              <div className="flex gap-2">
                <input
                  type="number"
                  placeholder="Interval (seconds)"
                  value={formData.interval}
                  onChange={(e) => setFormData({ ...formData, interval: parseInt(e.target.value) || 30 })}
                  className="bg-slate-900 border border-slate-600 rounded-lg px-4 py-2 focus:outline-none focus:border-blue-500 flex-1"
                  min="10"
                />
                <button onClick={handleAddSite} className="bg-green-600 hover:bg-green-700 px-6 py-2 rounded-lg transition">
                  Add
                </button>
              </div>
            </div>
          </div>
        )}

        {/* Sites List */}
        <div className="grid grid-cols-1 gap-6">
          {sites.map(site => (
            <div key={site.id} className="bg-slate-800/50 backdrop-blur border border-slate-700 rounded-xl p-6">
              <div className="flex items-start justify-between mb-4">
                <div className="flex-1">
                  <div className="flex items-center gap-3 mb-2">
                    <h3 className="text-xl font-semibold">{site.siteName}</h3>
                    <span className={`px-3 py-1 rounded-full text-sm font-medium ${
                      site.currentStatus === 'up' ? 'bg-green-500/20 text-green-400' :
                      site.currentStatus === 'down' ? 'bg-red-500/20 text-red-400' :
                      'bg-slate-500/20 text-slate-400'
                    }`}>
                      {site.currentStatus.toUpperCase()}
                    </span>
                  </div>
                  <p className="text-slate-400">{site.companyName}</p>
                  <p className="text-slate-500 text-sm">{site.siteUrl}</p>
                </div>
                <div className="flex gap-2">
                  <button
                    onClick={() => setPreviewSite(site)}
                    className="p-2 bg-purple-600 hover:bg-purple-700 rounded-lg transition"
                    title="Preview Site"
                  >
                    <Eye className="w-5 h-5" />
                  </button>
                  <button
                    onClick={() => handleTestSite(site)}
                    className="p-2 bg-blue-600 hover:bg-blue-700 rounded-lg transition"
                    title="Test Now"
                  >
                    <RefreshCw className="w-5 h-5" />
                  </button>
                  {monitoring[site.id] ? (
                    <button
                      onClick={() => stopMonitoring(site.id)}
                      className="p-2 bg-yellow-600 hover:bg-yellow-700 rounded-lg transition"
                      title="Stop Monitoring"
                    >
                      <Activity className="w-5 h-5" />
                    </button>
                  ) : (
                    <button
                      onClick={() => startMonitoring(site)}
                      className="p-2 bg-green-600 hover:bg-green-700 rounded-lg transition"
                      title="Start Monitoring"
                    >
                      <Play className="w-5 h-5" />
                    </button>
                  )}
                  <button
                    onClick={() => deleteSite(site.id)}
                    className="p-2 bg-red-600 hover:bg-red-700 rounded-lg transition"
                    title="Delete"
                  >
                    <Trash2 className="w-5 h-5" />
                  </button>
                </div>
              </div>

              {/* Site Preview Window */}
              <div className="bg-slate-900/80 rounded-lg p-3 mb-4 border border-slate-600">
                <div className="flex items-center justify-between mb-2">
                  <div className="flex items-center gap-2 text-sm text-slate-400">
                    <Eye className="w-4 h-4" />
                    <span>Browsing Preview</span>
                  </div>
                  <a 
                    href={site.siteUrl} 
                    target="_blank" 
                    rel="noopener noreferrer"
                    className="text-blue-400 hover:text-blue-300 transition"
                  >
                    <ExternalLink className="w-4 h-4" />
                  </a>
                </div>
                <div className="bg-white rounded overflow-hidden" style={{ height: '200px' }}>
                  <iframe
                    src={site.siteUrl}
                    className="w-full h-full border-0"
                    title={`Preview of ${site.siteName}`}
                    sandbox="allow-same-origin allow-scripts"
                  />
                </div>
              </div>

              {/* Live Stats */}
              <div className="grid grid-cols-2 md:grid-cols-4 gap-4 mb-4">
                <div className="bg-slate-900/50 rounded-lg p-3">
                  <p className="text-slate-400 text-xs mb-1">Uptime</p>
                  <p className="text-lg font-semibold text-green-400">{site.uptime}%</p>
                </div>
                <div className="bg-slate-900/50 rounded-lg p-3">
                  <p className="text-slate-400 text-xs mb-1">Response Time</p>
                  <p className="text-lg font-semibold">{site.responseTime || 0}ms</p>
                </div>
                <div className="bg-slate-900/50 rounded-lg p-3">
                  <p className="text-slate-400 text-xs mb-1">Check Interval</p>
                  <p className="text-lg font-semibold">{site.interval}s</p>
                </div>
                <div className="bg-slate-900/50 rounded-lg p-3">
                  <p className="text-slate-400 text-xs mb-1">Total Downtime</p>
                  <p className="text-lg font-semibold text-red-400">
                    {formatDuration(site.totalDowntime || 0)}
                  </p>
                </div>
              </div>

              {/* Response Time Graph */}
              {site.history && site.history.length > 0 && (
                <div className="bg-slate-900/50 rounded-lg p-4 mb-4">
                  <p className="text-sm text-slate-400 mb-3">Response Time History</p>
                  <div className="flex items-end gap-1 h-24">
                    {site.history.slice(-30).map((h, i) => (
                      <div
                        key={i}
                        className="flex-1 bg-gradient-to-t from-blue-600 to-blue-400 rounded-t"
                        style={{
                          height: `${Math.min((h.responseTime / 2000) * 100, 100)}%`,
                          opacity: h.status === 'up' ? 1 : 0.3
                        }}
                        title={`${h.responseTime}ms - ${h.status}`}
                      />
                    ))}
                  </div>
                </div>
              )}

              {/* Downtime Report */}
              {site.downtimes && site.downtimes.length > 0 && (
                <div className="bg-slate-900/50 rounded-lg p-4">
                  <p className="text-sm text-slate-400 mb-3 flex items-center gap-2">
                    <Clock className="w-4 h-4" />
                    Recent Downtime Events
                  </p>
                  <div className="space-y-2">
                    {site.downtimes.slice(-5).reverse().map((dt, i) => (
                      <div key={i} className="flex justify-between text-sm">
                        <span className="text-slate-300">
                          {new Date(dt.start).toLocaleString()}
                        </span>
                        <span className="text-red-400 font-medium">
                          Duration: {formatDuration(dt.duration)}
                        </span>
                      </div>
                    ))}
                  </div>
                </div>
              )}

              {site.lastChecked && (
                <p className="text-xs text-slate-500 mt-4">
                  Last checked: {new Date(site.lastChecked).toLocaleString()}
                </p>
              )}
            </div>
          ))}
        </div>

        {sites.length === 0 && (
          <div className="text-center py-12 text-slate-400">
            <Activity className="w-16 h-16 mx-auto mb-4 opacity-50" />
            <p className="text-lg">No websites added yet</p>
            <p className="text-sm">Click "Add Site" to start monitoring</p>
          </div>
        )}
      </div>

      {/* Preview Modal */}
      {previewSite && (
        <div 
          className="fixed inset-0 bg-black/80 backdrop-blur-sm flex items-center justify-center z-50 p-6"
          onClick={() => setPreviewSite(null)}
        >
          <div 
            className="bg-slate-800 rounded-xl border border-slate-700 w-full max-w-5xl"
            onClick={(e) => e.stopPropagation()}
          >
            <div className="flex items-center justify-between p-4 border-b border-slate-700">
              <div className="flex items-center gap-3">
                <Eye className="w-5 h-5 text-purple-400" />
                <div>
                  <h3 className="font-semibold">{previewSite.siteName}</h3>
                  <p className="text-sm text-slate-400">Browsing Preview</p>
                </div>
              </div>
              <div className="flex items-center gap-2">
                <a 
                  href={previewSite.siteUrl} 
                  target="_blank" 
                  rel="noopener noreferrer"
                  className="px-3 py-1 bg-blue-600 hover:bg-blue-700 rounded-lg text-sm transition flex items-center gap-2"
                >
                  Open Site <ExternalLink className="w-4 h-4" />
                </a>
                <button
                  onClick={() => setPreviewSite(null)}
                  className="px-3 py-1 bg-slate-700 hover:bg-slate-600 rounded-lg text-sm transition"
                >
                  Close
                </button>
              </div>
            </div>
            <div className="bg-white" style={{ height: '70vh' }}>
              <iframe
                src={previewSite.siteUrl}
                className="w-full h-full border-0"
                title={`Preview of ${previewSite.siteName}`}
                sandbox="allow-same-origin allow-scripts allow-popups allow-forms"
              />
            </div>
          </div>
        </div>
      )}
    </div>
  );
}
