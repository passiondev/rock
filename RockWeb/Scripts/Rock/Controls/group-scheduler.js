(function ($) {
    'use strict';
    window.Rock = window.Rock || {};
    Rock.controls = Rock.controls || {};

    /** JS helper for the groupScheduler */
    Rock.controls.groupScheduler = (function () {
        var exports = {
            /** initializes the javascript for the groupScheduler */
            initialize: function (options) {
                if (!options.id) {
                    throw 'id is required';
                }

                var $control = $('#' + options.id);
                var scheduledPersonAssignUrl = Rock.settings.get('baseUrl') + 'api/Attendances/ScheduledPersonAssign';


                var self = this;

                // initialize dragula
                var containers = [];

                // add the resource list as a dragular container
                containers.push($control.find('.js-scheduler-source-container')[0]);

                // add all the locations as dragula containers
                var targets = $control.find('.js-scheduler-target-container').toArray();

                $.each(targets, function (i) {
                    containers.push(targets[i]);
                });

                self.resourceListDrake = dragula(containers, {
                    isContainer: function (el) {
                        return false;
                    },
                    copy: function (el, source) {
                        return false;
                    },
                    accepts: function (el, target) {
                        return true;
                    },
                    invalid: function (el, handle) {
                        return false;
                    },
                    ignoreInputTextSelection: true
                })
                    .on('drag', function (el) {
                        if (self.resourceScroll) {
                            // disable the scroller while dragging so that the scroller doesn't move while we are dragging
                            self.resourceScroll.disable();
                        }
                        $('body').addClass('state-drag');
                    })
                    .on('dragend', function (el) {
                        if (self.resourceScroll) {
                            // reenable the scroller when done dragging
                            self.resourceScroll.enable();
                        }
                        $('body').removeClass('state-drag');
                    })
                    .on('drop', function (el, target, source, sibling) {
                        if (target.classList.contains('js-scheduler-source-container')) {
                            $(el).attr('data-state', 'unassigned');
                        }
                        else {
                            debugger
                            $(el).attr('data-state', 'assigned');
                            var personId = $(el).find('.js-resource-personId').val();
                            var attendanceOccurrenceId = $(target).closest('.js-location').find('.js-attendanceoccurrence-id').val();
                            $.ajax({
                                method: "PUT",
                                url: scheduledPersonAssignUrl + '?personId=' + personId + '&attendanceOccurrenceId=' + attendanceOccurrenceId
                            }).done(function (scheduledAttendance) {
                                // TODO
                                self.setScheduledPersonHtml(el, scheduledAttendance.ScheduledToAttend, scheduledAttendance.DeclineReasonValueId)
                                debugger
                            }).fail(function (a, b, c) {
                                // TODO
                                debugger
                            })
                        }
                        self.trimSourceContainer();
                    });

                // initialize scrollbar
                var $scrollContainer = $control.find('.js-resource-scroller');
                var $scrollIndicator = $control.find('.track');
                self.resourceScroll = new IScroll($scrollContainer[0], {
                    mouseWheel: true,
                    indicators: {
                        el: $scrollIndicator[0],
                        interactive: true,
                        resize: false,
                        listenY: true,
                        listenX: false,
                    },
                    preventDefaultException: { tagName: /.*/ }
                });

                this.trimSourceContainer();
                this.initializeEventHandlers();
                this.populateScheduled($control.find('.js-locations'));
            },
            /** trims the source container if it just has whitespace, so that the :empty css selector works */
            trimSourceContainer: function () {
                // if js-scheduler-source-container just has whitespace in it, trim it so that the :empty css selector works
                var $sourceContainer = $('.js-scheduler-source-container');
                if (($.trim($sourceContainer.html()) == "")) {
                    $sourceContainer.html("");
                }
            },
            /**  */
            setScheduledPersonHtml: function (el, scheduledToAttend, declineReasonValueId) {
                // pull-left badge badge-info badge-circle js-legend-badge
                debugger
            },
            /** */
            populateScheduled: function (container) {
                debugger
                var getScheduledUrl = Rock.settings.get('baseUrl') + 'api/Attendances/GetScheduled';
                var locationEls = $(".js-location", container).toArray();
                $.each(locationEls, function (i) {
                    var $location = $(locationEls[i]);
                    var attendanceOccurrenceId = $location.find('.js-attendanceoccurrence-id').val();
                    var $schedulerTargetContainer = $location.find('.js-scheduler-target-container');
                    $.get(getScheduledUrl + '?attendanceOccurrenceId=' + attendanceOccurrenceId, function (scheduledAttendanceItems) {
                        $schedulerTargetContainer.html('');
                        $.each(scheduledAttendanceItems, function (i) {
                            var scheduledAttendanceItem = scheduledAttendanceItems[i];
                            var $scheduleResourceDiv = $('.js-assigned-resource-template').find('.js-resource').clone();
                            $scheduleResourceDiv.find('.js-resource-status').data('data-status', scheduledAttendanceItem.Status);
                            $scheduleResourceDiv.find('.js-resource-name').text(scheduledAttendanceItem.PersonName);
                            $schedulerTargetContainer.append($scheduleResourceDiv);
                        });
                    });
                });
            },
            /**  */
            initializeEventHandlers: function () {
                var self = this;
                // TODO, needed?
            }
        };

        return exports;
    }());
}(jQuery));



