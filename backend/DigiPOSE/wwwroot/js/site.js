/**
 * Global Dynamic AJAX Modal Form Engine for ERP Architecture
 * Address: Unobtrusive Validation Parsing, Anti-Forgery Tokens, DRY Event Delegation, & Double-Submit Guard
 */
$(document).ready(function () {

    // Helper: Lock form and disable submit button with loading spinner
    function lockFormSubmit($form) {
        if ($form.data('submitting') === true) {
            return false;
        }
        $form.data('submitting', true);

        var $submitBtns = $form.find('button[type="submit"], input[type="submit"], .btn-cyber-primary[type="submit"], .btn-cyber-success[type="submit"], .btn-cyber-danger[type="submit"]');
        $submitBtns.prop('disabled', true).addClass('disabled');

        $submitBtns.each(function () {
            var $btn = $(this);
            if (!$btn.data('orig-html')) {
                $btn.data('orig-html', $btn.html());
            }
            $btn.html('<i class="fa-solid fa-spinner fa-spin me-2"></i> PROCESSING...');
        });

        return true;
    }

    // Helper: Unlock form and restore submit button
    function unlockFormSubmit($form) {
        $form.data('submitting', false);
        var $submitBtns = $form.find('button[type="submit"], input[type="submit"], .btn-cyber-primary[type="submit"], .btn-cyber-success[type="submit"], .btn-cyber-danger[type="submit"]');
        $submitBtns.prop('disabled', false).removeClass('disabled');

        $submitBtns.each(function () {
            var $btn = $(this);
            if ($btn.data('orig-html')) {
                $btn.html($btn.data('orig-html'));
            }
        });
    }

    // 1. GLOBAL GET ENGINE: Triggered by any element with class '.btn-show-modal'
    $(document).on('click', '.btn-show-modal', function (e) {
        e.preventDefault();
        var $btn = $(this);
        var url = $btn.data('url') || $btn.attr('href');

        if (!url) return;

        $.ajax({
            url: url,
            type: 'GET',
            headers: { "X-Requested-With": "XMLHttpRequest" },
            success: function (htmlContent) {
                var $container = $('#globalModalContainer');
                $container.html(htmlContent);

                // RE-PARSE UNOBTRUSIVE VALIDATION FOR DYNAMIC HTML IF AVAILABLE
                if ($.validator && $.validator.unobtrusive) {
                    $.validator.unobtrusive.parse($container);
                }

                var modalEl = document.getElementById('globalModal');
                if (modalEl) {
                    var modal = bootstrap.Modal.getInstance(modalEl) || new bootstrap.Modal(modalEl);
                    modal.show();
                }
            },
            error: function (xhr) {
                var errorMsg = (xhr.responseJSON && xhr.responseJSON.message) 
                    ? xhr.responseJSON.message 
                    : "Failed to load view (" + xhr.status + " " + xhr.statusText + ").";

                if (typeof showCyberNotify === 'function') {
                    showCyberNotify('System Fault', errorMsg, 'error');
                } else {
                    alert(errorMsg);
                }
            }
        });
    });

    // 2. GLOBAL POST ENGINE: Triggered by any form with class '.ajax-modal-form'
    $(document).on('submit', '.ajax-modal-form', function (e) {
        e.preventDefault();
        var $form = $(this);

        // CLIENT-SIDE VALIDATION CHECK IF AVAILABLE
        if ($form.valid && !$form.valid()) {
            return false;
        }

        // DOUBLE-SUBMIT GUARD: Lock form immediately
        if (!lockFormSubmit($form)) {
            return false;
        }

        var isMultipart = $form.attr('enctype') === 'multipart/form-data';
        var ajaxData, processDataVal, contentTypeVal;

        if (isMultipart) {
            ajaxData = new FormData($form[0]);
            processDataVal = false;
            contentTypeVal = false;
        } else {
            ajaxData = $form.serialize();
            processDataVal = true;
            contentTypeVal = 'application/x-www-form-urlencoded; charset=UTF-8';
        }

        $.ajax({
            url: $form.attr('action'),
            type: 'POST',
            data: ajaxData,
            processData: processDataVal,
            contentType: contentTypeVal,
            headers: { "X-Requested-With": "XMLHttpRequest" },
            success: function (response) {
                // CASE A: SERVER RETURNS JSON (SUCCESSFUL TRANSACTION)
                if (typeof response === 'object' && response.success) {
                    var modalEl = document.getElementById('globalModal');
                    if (modalEl) {
                        var modalInstance = bootstrap.Modal.getInstance(modalEl);
                        if (modalInstance) modalInstance.hide();
                    }

                    if (typeof showCyberNotify === 'function') {
                        showCyberNotify('Success', response.message || 'Action executed successfully.', 'success');
                    }
                    
                    setTimeout(function () {
                        location.reload();
                    }, 600);
                } 
                // CASE B: SERVER RETURNS JSON (BUSINESS RULE FAILURE)
                else if (typeof response === 'object' && !response.success) {
                    unlockFormSubmit($form);
                    if (typeof showCyberNotify === 'function') {
                        showCyberNotify('Validation Fault', response.message, 'green');
                    }
                }
                // CASE C: SERVER RETURNS HTML PARTIAL VIEW (MODELSTATE INVALID WITH INLINE ERRORS)
                else {
                    var $container = $('#globalModalContainer');
                    $container.html(response);

                    // RE-PARSE UNOBTRUSIVE VALIDATION FOR RETURNED ERROR PARTIAL VIEW
                    if ($.validator && $.validator.unobtrusive) {
                        $.validator.unobtrusive.parse($container);
                    }
                }
            },
            error: function (xhr) {
                unlockFormSubmit($form);
                var errorMsg = (xhr.responseJSON && xhr.responseJSON.message) 
                    ? xhr.responseJSON.message 
                    : "An error occurred during submission (" + xhr.status + ").";

                if (typeof showCyberNotify === 'function') {
                    showCyberNotify('Execution Error', errorMsg, 'error');
                } else {
                    alert(errorMsg);
                }
            }
        });
    });

    // 3. GLOBAL ACTIVE TOGGLE ENGINE: Handled with Cyber-Cinematic Modal Confirmation (Green/Orange/Red)
    $(document).on('submit', '.toggle-active-form', function (e) {
        e.preventDefault();
        var form = this;
        var $form = $(form);
        var $btn = $form.find('button');

        if ($form.data('submitting') === true) {
            return false;
        }

        var isCurrentlyActive = $btn.hasClass('bg-success');
        var themeType = isCurrentlyActive ? 'orange' : 'green';
        var actionTitle = isCurrentlyActive ? 'CONFIRM DEACTIVATION' : 'CONFIRM ACTIVATION';
        var actionMsg = isCurrentlyActive 
            ? 'Are you sure you want to DEACTIVATE this record? It will be marked as inactive in the system telemetry.' 
            : 'Are you sure you want to ACTIVATE this record?';
        var btnText = isCurrentlyActive ? 'DEACTIVATE' : 'ACTIVATE';

        var executeToggle = function() {
            if (!lockFormSubmit($form)) {
                return false;
            }

            var tokenInput = form.querySelector('[name=__RequestVerificationToken]');
            var token = (tokenInput && tokenInput.value) ? tokenInput.value : ($('input[name="__RequestVerificationToken"]').first().val() || '');
            
            var postData = $form.serialize();
            if (token && (!postData || postData.indexOf('__RequestVerificationToken') === -1)) {
                postData = (postData ? postData + '&' : '') + '__RequestVerificationToken=' + encodeURIComponent(token);
            }

            $.ajax({
                url: form.action,
                type: 'POST',
                data: postData,
                headers: { "X-Requested-With": "XMLHttpRequest", "RequestVerificationToken": token },
                success: function (d) {
                    if (typeof d === 'object' && d.success) {
                        if (typeof showCyberNotify === 'function') {
                            showCyberNotify('SYSTEM STATUS UPDATED', d.message || 'Status updated successfully.', 'success');
                        }
                        setTimeout(function () { location.reload(); }, 600);
                    } else {
                        unlockFormSubmit($form);
                        var errMsg = (d && d.message) ? d.message : 'Operation failed.';
                        if (typeof showCyberNotify === 'function') {
                            showCyberNotify('OPERATION FAULT', errMsg, 'error');
                        }
                    }
                },
                error: function (xhr) {
                    unlockFormSubmit($form);
                    if (typeof showCyberNotify === 'function') {
                        showCyberNotify('NETWORK FAULT', 'Communication error with server.', 'green');
                    }
                }
            });
        };

        if (typeof showCyberConfirm === 'function') {
            showCyberConfirm(actionTitle, actionMsg, themeType, executeToggle, btnText, 'CANCEL');
        } else {
            if (confirm(actionMsg)) {
                executeToggle();
            }
        }
    });

    // 4. GLOBAL CATCH-ALL FORM SUBMIT GUARD FOR ALL OTHER FORMS
    $(document).on('submit', 'form:not(.ajax-modal-form):not(.toggle-active-form)', function (e) {
        var $form = $(this);

        if ($form.valid && !$form.valid()) {
            return false;
        }

        if ($form.data('submitting') === true) {
            e.preventDefault();
            return false;
        }

        lockFormSubmit($form);
    });

    // 5. GLOBAL DATATABLES CYBER HUD OVERRIDES & INITIALIZATION
    if ($.fn.DataTable) {
        var cyberProcHtml = `
            <div class="cyber-proc-box">
                <div class="cyber-proc-header" style="background:#00FF00; color:#000000;">SEARCHING <span class="cyber-proc-dots">...</span></div>
                <div class="cyber-proc-content">
                    <div class="cyber-proc-status-row">
                        <span class="cyber-proc-status-title" style="color:#00FF00;">STATUS:</span>
                        <span class="cyber-proc-status-pct cyber-proc-pct-val" style="color:#00FF00;">0%</span>
                    </div>
                    <div class="cyber-proc-bar-frame" style="border:1px solid #00FF00;">
                        <div class="cyber-proc-bar-fill cyber-proc-fill-val" style="width: 0%;"></div>
                    </div>
                    <div class="cyber-proc-logs">
                        <div class="cyber-proc-log-line" style="color:#00FF00;">> TELEMETRY: SEARCHING DATABASE CLUSTER...</div>
                    </div>
                </div>
            </div>`;

        $.extend(true, $.fn.dataTable.defaults, {
            language: {
                processing: cyberProcHtml,
                search: "SEARCH:",
                lengthMenu: "SHOW _MENU_ RECORDS",
                info: "SHOWING _START_ TO _END_ OF _TOTAL_ ENTITIES",
                infoEmpty: "SHOWING 0 TO 0 OF 0 ENTITIES",
                infoFiltered: "(FILTERED FROM _MAX_ TOTAL ENTITIES)",
                paginate: {
                    first: "FIRST",
                    last: "LAST",
                    next: "NEXT",
                    previous: "PREV"
                }
            }
        });

        // Dynamic Processing HUD Real-Time Progress Ticker (0% -> 100%)
        $(document).on('processing.dt', function (e, settings, processing) {
            var $wrapper = $(settings.nTableWrapper);
            var $proc = $wrapper.find('.dataTables_processing');
            if (!$proc.length) return;

            var timer = $proc.data('cyber-timer');
            if (timer) clearInterval(timer);

            if (processing) {
                var pct = 10;
                var logs = [
                    "> SYSTEM_INIT: CONNECTING SQL CLUSTER...",
                    "> QUERY_EXEC: FILTERING INDEXED DATASET...",
                    "> DATA_STREAM: DE-SERIALIZING RECORD STREAM...",
                    "> SECURITY_CHECK: VERIFYING TELEMETRY SIGNATURE..."
                ];
                var logIdx = 0;

                $proc.find('.cyber-proc-pct-val').text(pct + '%');
                $proc.find('.cyber-proc-fill-val').css('width', pct + '%');
                $proc.find('.cyber-proc-log-line').text(logs[0]);

                timer = setInterval(function () {
                    pct += Math.floor(Math.random() * 15) + 10;
                    if (pct > 92) pct = 92;

                    $proc.find('.cyber-proc-pct-val').text(pct + '%');
                    $proc.find('.cyber-proc-fill-val').css('width', pct + '%');

                    if (logIdx < logs.length) {
                        $proc.find('.cyber-proc-log-line').text(logs[logIdx]);
                        logIdx++;
                    }
                }, 80);

                $proc.data('cyber-timer', timer);
                $proc.css('display', 'block');
            } else {
                // Complete progress bar to 100% on processing finish
                $proc.find('.cyber-proc-pct-val').text('100%');
                $proc.find('.cyber-proc-fill-val').css('width', '100%');
                $proc.find('.cyber-proc-log-line').text('> STATUS: 100% SEARCH COMPLETED');

                setTimeout(function () {
                    $proc.fadeOut(200, function() {
                        // Reset fill to 0% for next search
                        $proc.find('.cyber-proc-pct-val').text('0%');
                        $proc.find('.cyber-proc-fill-val').css('width', '0%');
                    });
                }, 300);
            }
        });
    }

});
