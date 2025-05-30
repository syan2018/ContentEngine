// ContentEngine WebApp JavaScript Functions

/**
 * 获取元素的滚动位置
 * @param {string} elementId - 元素ID
 * @returns {object} - 滚动位置 {x, y}
 */
window.getScrollPosition = function(elementId) {
    const element = elementId ? document.getElementById(elementId) : window;
    if (element === window) {
        return { x: window.scrollX, y: window.scrollY };
    } else if (element) {
        return { x: element.scrollLeft, y: element.scrollTop };
    }
    return { x: 0, y: 0 };
};

/**
 * 设置元素的滚动位置
 * @param {string} elementId - 元素ID
 * @param {number} x - 水平滚动位置
 * @param {number} y - 垂直滚动位置
 */
window.setScrollPosition = function(elementId, x, y) {
    const element = elementId ? document.getElementById(elementId) : window;
    if (element === window) {
        window.scrollTo(x, y);
    } else if (element) {
        element.scrollLeft = x;
        element.scrollTop = y;
    }
};

/**
 * 检查元素是否在视口中
 * @param {string} elementId - 元素ID
 * @returns {boolean} - 是否在视口中
 */
window.isElementInViewport = function(elementId) {
    const element = document.getElementById(elementId);
    if (!element) return false;
    
    const rect = element.getBoundingClientRect();
    return (
        rect.top >= 0 &&
        rect.left >= 0 &&
        rect.bottom <= (window.innerHeight || document.documentElement.clientHeight) &&
        rect.right <= (window.innerWidth || document.documentElement.clientWidth)
    );
};

/**
 * 滚动到页面顶部
 */
window.scrollToTop = function() {
    window.scrollTo({ top: 0, behavior: 'smooth' });
};

/**
 * 获取浏览器信息
 * @returns {object} - 浏览器信息
 */
window.getBrowserInfo = function() {
    const userAgent = navigator.userAgent;
    const isChrome = /Chrome/.test(userAgent) && /Google Inc/.test(navigator.vendor);
    const isFirefox = /Firefox/.test(userAgent);
    const isSafari = /Safari/.test(userAgent) && /Apple Computer/.test(navigator.vendor);
    const isEdge = /Edg/.test(userAgent);
    
    return {
        userAgent: userAgent,
        isChrome: isChrome,
        isFirefox: isFirefox,
        isSafari: isSafari,
        isEdge: isEdge,
        isMobile: /Mobi|Android/i.test(userAgent),
        language: navigator.language,
        platform: navigator.platform
    };
};

// 初始化函数
window.initializeApp = function() {
    console.log('ContentEngine WebApp JavaScript 已加载');
    
    // 可以在这里添加全局初始化逻辑
    // 例如：设置全局错误处理、性能监控等
    
    // 全局错误处理
    window.addEventListener('error', function(event) {
        console.error('JavaScript 错误:', event.error);
    });
    
    // 全局未处理的 Promise 拒绝
    window.addEventListener('unhandledrejection', function(event) {
        console.error('未处理的 Promise 拒绝:', event.reason);
    });
};

// 当 DOM 加载完成时初始化
if (document.readyState === 'loading') {
    document.addEventListener('DOMContentLoaded', window.initializeApp);
} else {
    window.initializeApp();
} 