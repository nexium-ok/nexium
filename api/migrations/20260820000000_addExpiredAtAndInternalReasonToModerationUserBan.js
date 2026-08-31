/**
 * @param {import('knex')} knex
 */
exports.up = async (knex) => {
    await knex.schema.alterTable('moderation_user_ban', (t) => {
        t.dateTime('expired_at').nullable().defaultTo(null);
        t.string('internal_reason', 4096).nullable().defaultTo(null);
    });
};

exports.down = async (knex) => {
    await knex.schema.alterTable('moderation_user_ban', (t) => {
        t.dropColumn('expired_at');
        t.dropColumn('internal_reason');
    });
};