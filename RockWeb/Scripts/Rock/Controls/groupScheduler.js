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

                var self = this;

                var $control = $('#' + options.id);

                if ($control.length == 0) {
                    return;
                }

                self.$groupScheduler = $control;
                self.$resourceList = $('.group-scheduler-resourcelist', $control);
                self.$additionalPersonIds = $('.js-resource-additional-person-ids', self.$resourceList)

                // initialize dragula
                var containers = [];

                // add the resource list as a dragular container
                containers.push($control.find('.js-scheduler-source-container')[0]);

                // add all the occurrences (locations) as dragula containers
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
                        // ignore drag if they are clicking on the actions menu of a resource
                        var isMenu = $(el).closest('.js-resource-actions').length;
                        return isMenu;
                    },
                    ignoreInputTextSelection: true
                })
                    .on('drag', function (el) {
                        if (self.resourceScroll) {
                            // disable the scroller while dragging so that the scroller doesn't move while we are dragging
                            //self.resourceScroll.disable();
                        }
                        $('body').addClass('state-drag');
                    })
                    .on('dragend', function (el) {
                        if (self.resourceScroll) {
                            // reenable the scroller when done dragging
                            //self.resourceScroll.enable();
                        }
                        $('body').removeClass('state-drag');
                    })
                    .on('drop', function (el, target, source, sibling) {
                        if (target.classList.contains('js-scheduler-source-container')) {
                            // deal with the resource that was dragged back into the unassigned resources
                            var $unassignedResource = $(el);
                            $unassignedResource.attr('data-state', 'unassigned');

                            var personId = $unassignedResource.attr('data-person-id')

                            var additionalPersonIds = self.$additionalPersonIds.val().split(',');
                            additionalPersonIds.push(personId);

                            self.$additionalPersonIds.val(additionalPersonIds);
                            var scheduledPersonUnassignUrl = Rock.settings.get('baseUrl') + 'api/Attendances/ScheduledPersonUnassign';

                            var attendanceId = $unassignedResource.attr('data-attendance-id')
                            $.ajax({
                                method: "PUT",
                                url: scheduledPersonUnassignUrl + '?attendanceId=' + attendanceId
                            }).done(function (scheduledAttendance) {
                                // after unassigning a resource, repopulate the list of unassigned resources
                                self.populateSchedulerResources(self.$resourceList);

                                // after unassigning a resource, repopulate the list of resources for the occurrence
                                var $occurrence = $(source).closest('.js-scheduled-occurrence');
                                self.populateScheduledOccurrence($occurrence);
                            }).fail(function (a, b, c) {
                                // TODO
                                debugger
                            })
                        }
                        else {
                            // deal with the resource that was dragged into an assigned occurrence (location)
                            var scheduledPersonAssignUrl = Rock.settings.get('baseUrl') + 'api/Attendances/ScheduledPersonAssign';
                            var $assignedResource = $(el);
                            $assignedResource.attr('data-state', 'assigned');
                            var personId = $assignedResource.attr('data-person-id')
                            var attendanceOccurrenceId = $(target).closest('.js-scheduled-occurrence').find('.js-attendanceoccurrence-id').val();
                            $.ajax({
                                method: "PUT",
                                url: scheduledPersonAssignUrl + '?personId=' + personId + '&attendanceOccurrenceId=' + attendanceOccurrenceId
                            }).done(function (scheduledAttendance) {
                                // after adding a resource, repopulate the list of resources for the occurrence
                                var $occurrence = $(el).closest('.js-scheduled-occurrence');
                                self.populateScheduledOccurrence($occurrence);
                            }).fail(function (a, b, c) {
                                // TODO
                                debugger
                            })
                        }
                        self.trimSourceContainer();
                    });

                // initialize scrollbar
                // var $scrollContainer = $control.find('.js-resource-scroller');
                // var $scrollIndicator = $control.find('.track');
                // self.resourceScroll = new IScroll($scrollContainer[0], {
                //     mouseWheel: true,
                //     indicators: {
                //         el: $scrollIndicator[0],
                //         interactive: true,
                //         resize: false,
                //         listenY: true,
                //         listenX: false,
                //     },
                //     preventDefaultException: { tagName: /.*/ }
                // });

                this.trimSourceContainer();
                this.initializeEventHandlers();

                self.populateSchedulerResources(self.$resourceList);

                var occurrenceEls = $(".js-scheduled-occurrence", $control).toArray();
                $.each(occurrenceEls, function (i) {
                    var $occurrence = $(occurrenceEls[i]);
                    self.populateScheduledOccurrence($occurrence);
                });
            },
            /** trims the source container if it just has whitespace, so that the :empty css selector works */
            trimSourceContainer: function () {
                // if js-scheduler-source-container just has whitespace in it, trim it so that the :empty css selector works
                var $sourceContainer = $('.js-scheduler-source-container');
                if (($.trim($sourceContainer.html()) == "")) {
                    $sourceContainer.html("");
                }
            },
            /** populates the assigned (requested/scheduled) resources for the occurrence div */
            populateScheduledOccurrence: function ($occurrence) {
                var getScheduledUrl = Rock.settings.get('baseUrl') + 'api/Attendances/GetScheduled';
                var attendanceOccurrenceId = $occurrence.find('.js-attendanceoccurrence-id').val();
                var $schedulerTargetContainer = $occurrence.find('.js-scheduler-target-container');

                var minimumCapacity = $occurrence.find('.js-minimum-capacity').val();
                var desiredCapacity = $occurrence.find('.js-desired-capacity').val();
                var maximumCapacity = $occurrence.find('.js-maximum-capacity').val();
                var $schedulingStatusContainer = $occurrence.find('.js-scheduling-status');

                var self = this;
                $.get(getScheduledUrl + '?attendanceOccurrenceId=' + attendanceOccurrenceId, function (scheduledAttendanceItems) {
                    $schedulerTargetContainer.html('');
                    var totalPending = 0;
                    var totalConfirmed = 0;
                    var totalDeclined = 0;

                    $.each(scheduledAttendanceItems, function (i) {
                        var scheduledAttendanceItem = scheduledAttendanceItems[i];

                        // add up status numbers
                        if (scheduledAttendanceItem.ConfirmationStatus == 'confirmed') {
                            totalConfirmed++;
                        } else if (scheduledAttendanceItem.ConfirmationStatus == 'Declined') {
                            totalDeclined++;
                        } else {
                            totalPending++;
                        }

                        var $resourceDiv = $('.js-assigned-resource-template').find('.js-resource').clone();
                        self.populateResourceDiv($resourceDiv, scheduledAttendanceItem, 'assigned');
                        $schedulerTargetContainer.append($resourceDiv);
                    });

                    var $statusLight = $schedulingStatusContainer.find('.js-scheduling-status-light');

                    if (minimumCapacity && (totalConfirmed < minimumCapacity)) {
                        $statusLight.attr('data-status', 'below-minimum');
                    }
                    else if (desiredCapacity && (totalConfirmed < desiredCapacity)) {
                        $statusLight.attr('data-status', 'below-desired');
                    }
                    else if (desiredCapacity && (totalConfirmed >= desiredCapacity)) {
                        $statusLight.attr('data-status', 'meets-desired');
                    }
                    else {
                        // no capacities defined, so just hide it
                        $statusLight.attr('data-status', 'none');
                    }

                    // set the progressbar max to the max capacity (if known), or just the sum of scheduled
                    var progressMax = maximumCapacity;
                    if (!maximumCapacity) {
                        progressMax = (totalPending + totalConfirmed + totalDeclined)
                    }

                    var confirmedPercent = !progressMax || (totalConfirmed*100 / progressMax);
                    var pendingPercent = !progressMax || (totalPending*100 / progressMax);
                    var declinedPercent = !progressMax || (totalDeclined * 100 / progressMax);
                    var minimumPercent = !progressMax || (minimumCapacity * 100 / progressMax);

                    var $progressMinimumIndicator = $schedulingStatusContainer.find('.js-minimum-indicator');
                    $progressMinimumIndicator
                        .attr('data-minimum-value', minimumCapacity)
                        .css({ 'margin-left': minimumCapacity + '%' });

                    $progressMinimumIndicator.toggle(minimumCapacity > 0);

                    var $progressConfirmed = $schedulingStatusContainer.find('.js-scheduling-progress-confirmed');
                    $progressConfirmed.css({ 'width': confirmedPercent + '%' });
                    $progressConfirmed.find('.js-progress-text-percent').val(confirmedPercent);

                    var $progressPending = $schedulingStatusContainer.find('.js-scheduling-progress-pending');
                    $progressPending.css({ 'width': pendingPercent + '%' });
                    $progressPending.find('.js-progress-text-percent').val(pendingPercent);

                    var $progressDeclined = $schedulingStatusContainer.find('.js-scheduling-progress-declined');
                    $progressDeclined.css({ 'width': declinedPercent + '%' });
                    $progressDeclined.find('.js-progress-text-percent').val(declinedPercent);


                });
            },
            /** populates the resource list with unassigned resources */
            populateSchedulerResources: function ($resourceList) {
                var self = this;
                var $resourceContainer = $('.js-scheduler-source-container', $resourceList);
                var getSchedulerResourcesUrl = Rock.settings.get('baseUrl') + 'api/Attendances/GetSchedulerResources';

                // javascript creates a non-empty [''] array on empty string, so only split if there are additionalPersonIds specified
                var additionalPersonIds = [];
                if (self.$additionalPersonIds.val() != '') {
                    additionalPersonIds = self.$additionalPersonIds.val().split(',');
                }

                var schedulerResourceParameters = {
                    AttendanceOccurrenceGroupId: Number($('.js-occurrence-group-id', $resourceList).val()),
                    AttendanceOccurrenceScheduleId: Number($('.js-occurrence-schedule-id', $resourceList).val()),
                    AttendanceOccurrenceOccurrenceDate: $('.js-occurrence-occurrence-date', $resourceList).val(),
                    ResourceGroupId: $('.js-resource-group-id', $resourceList).val(),
                    GroupMemberFilterType: $('.js-resource-groupmemberfiltertype', $resourceList).val(),
                    ResourceDataViewId: $('.js-resource-dataview-id', $resourceList).val(),
                    ResourceAdditionalPersonIds: additionalPersonIds
                };

                var $loadingNotification = $resourceList.find('.js-loading-notification');

                $resourceContainer.html(' ');
                $loadingNotification.fadeIn();

                $.ajax({
                    method: "POST",
                    url: getSchedulerResourcesUrl,
                    data: schedulerResourceParameters
                }).done(function (schedulerResources) {
                    var resourceContainerParent = $resourceContainer.parent();

                    // temporarily detach $resourceContainer to speed up adding the resourcedivs
                    $resourceContainer.detach();
                    $resourceContainer.html('');
                    var $resourceTemplate = $('.js-unassigned-resource-template').find('.js-resource');
                    for (var i = 0; i < schedulerResources.length; i++) {
                        var schedulerResource = schedulerResources[i];
                        var $resourceDiv = $resourceTemplate.clone();
                        self.populateResourceDiv($resourceDiv, schedulerResource, 'unassigned');
                        $resourceContainer.append($resourceDiv);
                    }

                    resourceContainerParent.append($resourceContainer);

                    setTimeout(function () {
                        $loadingNotification.hide();
                        //self.resourceScroll.refresh();
                    }, 0)

                }).fail(function (a, b, c) {
                    // TODO
                    $loadingNotification.hide();
                    debugger
                });

            },
            /**  populates the resource element (both assigned and unassigned) */
            populateResourceDiv: function ($resourceDiv, schedulerResource, state) {
                $resourceDiv.attr('data-state', state);
                $resourceDiv.attr('data-person-id', schedulerResource.PersonId);
                $resourceDiv.attr('data-has-conflict', schedulerResource.HasConflict);
                $resourceDiv.attr('data-has-blackout-conflict', schedulerResource.HasBlackoutConflict);

                $resourceDiv.find('.js-resource-name').text(schedulerResource.PersonName);
                if (schedulerResource.Note) {
                    $resourceDiv.find('.js-resource-note').text(schedulerResource.Note);
                }

                if (schedulerResource.ConflictNote) {
                    $resourceDiv.find('.js-resource-warning').text(schedulerResource.ConflictNote);
                }

                if (schedulerResource.LastAttendanceDateTime) {
                    var $lastAttendedDate = $resourceDiv.find('.js-resource-lastattendeddate');
                    $lastAttendedDate.attr('data-datetime', schedulerResource.LastAttendanceDateTime);
                    $lastAttendedDate.text(schedulerResource.LastAttendanceDateTimeFormatted);
                }


                if (schedulerResource.ConfirmationStatus) {
                    $resourceDiv.find('.js-resource-status').attr('data-status', schedulerResource.ConfirmationStatus);
                }

                // stuff that only applies to unassigned resource
                if (schedulerResource.IsAlreadyScheduledForGroup != null) {
                    $resourceDiv.attr('data-is-scheduled', schedulerResource.IsAlreadyScheduledForGroup);
                }

                // stuff that only applies to assigned resource
                if (schedulerResource.AttendanceId) {
                    $resourceDiv.attr('data-attendance-id', schedulerResource.AttendanceId);
                }
            },
            /**  */
            initializeEventHandlers: function () {
                var self = this;

                self.$groupScheduler.on('click', '.js-markconfirmed', function () {

                    var $resource = $(this).closest('.js-resource');
                    var attendanceId = $resource.attr('data-attendance-id');
                    var scheduledPersonConfirmUrl = Rock.settings.get('baseUrl') + 'api/Attendances/ScheduledPersonConfirm';

                    var attendanceId = $resource.attr('data-attendance-id')
                    $.ajax({
                        method: "PUT",
                        url: scheduledPersonConfirmUrl + '?attendanceId=' + attendanceId
                    }).done(function (scheduledAttendance) {
                        // after confirming a resource, repopulate the list of resources for this occurrence
                        var $occurrence = $resource.closest('.js-scheduled-occurrence');
                        self.populateScheduledOccurrence($occurrence);
                    }).fail(function (a, b, c) {
                        debugger
                    })
                });

            }
        };

        return exports;
    }());
}(jQuery));







