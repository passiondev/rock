(function ($) {
    'use strict';
    window.Rock = window.Rock || {};
    Rock.controls = Rock.controls || {};

    Rock.controls.groupScheduler = (function () {
        var exports = {
            initialize: function (options) {
                if (!options.id) {
                    throw 'id is required';
                }

                var self = this;
                self.dragulaParentContainer = $('#' + options.id);
                var containers = [];
                containers.push(self.dragulaParentContainer.find('.js-scheduler-source-container')[0]);
                var targets = self.dragulaParentContainer.find('.js-scheduler-target-container').toArray();

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
                        $('body').addClass('state-drag');
                    })
                    .on('dragend', function (el) {
                        $('body').removeClass('state-drag');
                    })
                    .on('drop', function (el, target, source, sibling) {
                        if (target.classList.contains('js-scheduler-source-container')) {
                            $(el).attr('data-state', 'unassigned');
                        }
                        else {
                            $(el).attr('data-state', 'assigned');
                        }
                    });

                this.initializeEventHandlers();
            },
            initializeEventHandlers: function () {
                var self = this;
                // TODO, needed?
            }
        };

        return exports;
    }());
}(jQuery));



