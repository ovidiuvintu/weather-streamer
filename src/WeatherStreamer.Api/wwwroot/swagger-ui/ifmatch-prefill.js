(function () {
  function setIfMatch(req) {
    try {
      var method = (req.init && (req.init.method || req.method)) || req.method;
      if (!method) return req;
      var m = method.toUpperCase();
      // Only intervene for DELETE and PATCH requests which require If-Match
      if (m !== 'DELETE' && m !== 'PATCH') return req;

      // Determine if If-Match is already present
      var headers = (req.init && req.init.headers) || req.headers;
      var hasIfMatch = false;
      if (headers) {
        if (typeof headers.get === 'function') {
          hasIfMatch = headers.get('If-Match') != null || headers.get('if-match') != null;
        } else {
          hasIfMatch = headers['If-Match'] != null || headers['if-match'] != null || headers['If-Match'.toLowerCase()] != null;
        }
      }
      if (hasIfMatch) return req;

      var url = req.url;
      // Perform a GET to the same resource to obtain ETag
      return fetch(url, { method: 'GET', credentials: 'same-origin' })
        .then(function (resp) {
          var etag = resp.headers.get('ETag') || resp.headers.get('etag') || null;
          if (!etag) return req;
          // Normalize ETag: remove weak prefix and surrounding quotes so If-Match contains raw token
          function normalizeEtag(e) {
            if (!e) return e;
            var v = e.trim();
            if (v.toUpperCase().startsWith('W/')) {
              v = v.substring(2);
            }
            if (v.length >= 2 && v.startsWith('"') && v.endsWith('"')) {
              v = v.substring(1, v.length - 1); // remove surrounding quotes
            }
            return v;
          }
          var raw = normalizeEtag(etag);
          if (!raw) return req;
          req.init = req.init || {};
          if (!req.init.headers) req.init.headers = {};
          // If headers is a Headers-like object
          if (typeof req.init.headers.set === 'function') {
            req.init.headers.set('If-Match', raw);
          } else if (typeof req.init.headers.append === 'function') {
            req.init.headers.append('If-Match', raw);
          } else {
            req.init.headers['If-Match'] = raw;
          }
          // Also set on req.headers for Swagger versions that read headers from there
          if (req.headers) {
            if (typeof req.headers.set === 'function') {
              req.headers.set('If-Match', raw);
            } else {
              req.headers['If-Match'] = raw;
            }
          }
          return req;
        })
        .catch(function () { return req; });
    } catch (e) {
      return req;
    }
  }

  function attach() {
    if (!window.ui || !window.ui.getConfigs) {
      setTimeout(attach, 200);
      return;
    }
    var configs = window.ui.getConfigs();
    var orig = configs.requestInterceptor;
    configs.requestInterceptor = function (req) {
      var res = orig ? orig(req) : req;
      if (res && typeof res.then === 'function') {
        return res.then(setIfMatch);
      }
      return setIfMatch(res);
    };
  }
  attach();
})();
